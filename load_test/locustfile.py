"""End-to-end load test for every implemented Round-Batman API route.

Each simulated user creates unique teams, runs one complete Round Robin
tournament (group stage -> knockout -> champion), exercises the explicit match
API, and then removes the data it created. Statistics are grouped by route
template instead of individual UUIDs.
"""

import logging
from argparse import ArgumentTypeError
from datetime import datetime
from time import perf_counter
from uuid import uuid4

from locust import HttpUser, between, events, task
from locust.exception import StopUser


def teams_per_tournament(value):
    number = int(value)
    if not 2 <= number <= 32:
        raise ArgumentTypeError("must be an integer between 2 and 32")
    return number


@events.init_command_line_parser.add_listener
def add_options(parser):
    parser.add_argument(
        "--teams-per-tournament",
        type=teams_per_tournament,
        default=8,
        help="Teams in each tournament (2-32, default 8).",
    )
    parser.add_argument(
        "--tournament-type",
        choices=("ROUND_ROBIN", "HYBRID"),
        default="HYBRID",
        help="Tournament format: pure round robin or round robin plus knockout.",
    )
    parser.add_argument(
        "--keep-data",
        action="store_true",
        help="Keep the main tournament and its teams after each scenario.",
    )


class ScenarioFailed(Exception):
    """A failed request or contract check; do not issue dependent requests."""


class RoundBatmanUser(HttpUser):
    wait_time = between(1, 3)

    def on_start(self):
        options = self.environment.parsed_options
        self.team_count = getattr(options, "teams_per_tournament", 8)
        self.tournament_type = getattr(options, "tournament_type", "HYBRID")
        self.keep_data = getattr(options, "keep_data", False)
        if not 2 <= self.team_count <= 32:
            message = (
                f"Invalid team count: {self.team_count}. Require between 2 and "
                "32 teams per tournament. Try --teams-per-tournament 8."
            )
            logging.error(message)
            raise StopUser(message)

    # ------------------------------------------------------------------ HTTP

    def request(self, method, path, name, status=200, payload=None,
                validate=None, timeout=60):
        with self.client.request(
            method, path, name=name, json=payload, timeout=timeout,
            catch_response=True, allow_redirects=False,
        ) as response:
            try:
                if response.status_code != status:
                    body = (getattr(response, "text", "") or "")[:300]
                    raise ValueError(
                        f"Expected HTTP {status}, got {response.status_code}: {body}"
                    )
                return validate(response) if validate else None
            except (ValueError, TypeError, KeyError) as error:
                response.failure(str(error))
                raise ScenarioFailed(f"{name}: {error}") from error

    def create(self, path, name, payload):
        def identifier(response):
            document = response.json()
            if not isinstance(document, dict):
                raise ValueError("Expected a JSON object")
            created = document.get("id")
            if not isinstance(created, str) or not created:
                raise ValueError("Response is missing the resource id")
            location = response.headers.get("Location", "")
            if not location.endswith(created):
                raise ValueError("Location header does not match the resource id")
            return created

        return self.request("POST", path, name, 201, payload, identifier)

    # ------------------------------------------------------------ validators

    @staticmethod
    def object_validator(expected):
        def validate(response):
            document = response.json()
            if not isinstance(document, dict):
                raise ValueError("Expected a JSON object")
            for key, value in expected.items():
                if document.get(key) != value:
                    raise ValueError(f"Unexpected {key} in response")
            return document

        return validate

    @staticmethod
    def list_contains(expected_ids):
        def validate(response):
            documents = response.json()
            if not isinstance(documents, list) or any(
                not isinstance(document, dict) for document in documents
            ):
                raise ValueError("Expected a JSON array of objects")
            actual = {document.get("id") for document in documents}
            if not set(expected_ids).issubset(actual):
                raise ValueError("Created resources missing from listing")
            return documents

        return validate

    @staticmethod
    def match_list(response):
        matches = response.json()
        if not isinstance(matches, list) or any(
            not isinstance(match, dict) for match in matches
        ):
            raise ValueError("Expected a JSON array of match objects")
        return matches

    def match_list_validator(self, expected_count=None, stage=None):
        def validate(response):
            matches = self.match_list(response)
            if expected_count is not None and len(matches) != expected_count:
                raise ValueError(
                    f"Expected {expected_count} matches, got {len(matches)}"
                )
            if stage is not None and any(
                match.get("stage") != stage for match in matches
            ):
                raise ValueError(f"Expected only {stage} matches")
            ids = [match.get("id") for match in matches]
            if any(not isinstance(value, str) or not value for value in ids):
                raise ValueError("Match is missing its id")
            if len(set(ids)) != len(ids):
                raise ValueError("Duplicate match ids in response")
            return matches

        return validate

    @staticmethod
    def recent_matches_validator(response, expected_count=None):
        matches = RoundBatmanUser.match_list(response)
        if len(matches) > 20:
            raise ValueError(f"Expected at most 20 recent matches, got {len(matches)}")
        if expected_count is not None and len(matches) != expected_count:
            raise ValueError(
                f"Expected {expected_count} recent completed matches, got {len(matches)}"
            )

        timestamps = []
        for match in matches:
            value = match.get("completedAt")
            if not isinstance(value, str) or not value:
                raise ValueError("Recent match is missing completedAt")
            try:
                timestamps.append(datetime.fromisoformat(value.replace("Z", "+00:00")))
            except ValueError as error:
                raise ValueError("Recent match has an invalid completedAt") from error
        if timestamps != sorted(timestamps, reverse=True):
            raise ValueError("Recent matches are not ordered newest first")
        return matches

    def generated_teams_validator(self, response):
        teams = response.json()
        if not isinstance(teams, list) or len(teams) != 1:
            raise ValueError(
                f"Expected 1 generated team, got "
                f"{len(teams) if isinstance(teams, list) else 'a non-list'}"
            )
        team = teams[0]
        if not isinstance(team, dict):
            raise ValueError("Expected a team object")
        team_id = team.get("id")
        if not isinstance(team_id, str) or not team_id:
            raise ValueError("Generated team is missing its id")
        if not team.get("name"):
            raise ValueError("Generated team is missing its name")
        return teams

    def group_with_teams(self, group_id, name, team_ids):
        def validate(response):
            document = self.object_validator(
                {"id": group_id, "name": name}
            )(response)
            members = document.get("teams")
            if not isinstance(members, list) or any(
                not isinstance(team, dict) for team in members
            ):
                raise ValueError("Expected a teams array")
            actual = [team.get("id") for team in members]
            if set(actual) != set(team_ids):
                raise ValueError("Group membership does not match assigned teams")
            return document

        return validate

    def finished_tournament(self, tournament_id, name, team_ids):
        def validate(response):
            document = self.object_validator(
                {"id": tournament_id, "name": name, "status": "FINISHED"}
            )(response)
            if document.get("format", {}).get("type") != self.tournament_type:
                raise ValueError("Tournament format does not match the selected type")
            champion = document.get("championTeamId")
            if champion not in team_ids:
                raise ValueError("Champion is not one of the tournament teams")
            if not document.get("championTeamName"):
                raise ValueError("Finished tournament is missing the champion name")
            matches = document.get("matches")
            if not isinstance(matches, list):
                raise ValueError("Finished tournament is missing its matches")
            has_knockout = any(match.get("stage") == "KNOCKOUT" for match in matches)
            if self.tournament_type == "HYBRID" and not has_knockout:
                raise ValueError("Hybrid tournament finished without knockout matches")
            if self.tournament_type == "ROUND_ROBIN" and has_knockout:
                raise ValueError("Pure round robin unexpectedly created knockout matches")
            return document

        return validate

    def simulated_matches_validator(self, response):
        matches = self.match_list(response)
        if not matches:
            raise ValueError("Simulation returned no matches")
        knockout = [match for match in matches if match.get("stage") == "KNOCKOUT"]
        if self.tournament_type == "HYBRID" and not knockout:
            raise ValueError("Hybrid simulation created no knockout matches")
        if self.tournament_type == "ROUND_ROBIN" and knockout:
            raise ValueError("Pure round robin created knockout matches")
        for match in knockout:
            if not match.get("isBye") and match.get("winner") is None:
                raise ValueError("Knockout match has no winner")
        return matches

    # --------------------------------------------------------------- scenario

    @task
    def full_lifecycle(self):
        started = perf_counter()
        error = None
        try:
            self.run_scenario()
        except ScenarioFailed as failure:
            error = failure
        # An interrupted/killed user must not report an incomplete scenario as successful.
        self.environment.events.request.fire(
            request_type="SCENARIO", name="Full API lifecycle",
            response_time=(perf_counter() - started) * 1000,
            response_length=0, exception=error, context=self.context(),
        )

    def run_scenario(self):
        self.request("GET", "/health", "GET /health")
        self.request("GET", "/health/db", "GET /health/db")

        run_id = uuid4().hex
        team_ids = self.create_teams(run_id)
        tournament_id = self.run_tournament(run_id, team_ids)
        self.run_explicit_match(run_id, team_ids)

        if not self.keep_data:
            self.cleanup(tournament_id, team_ids)

    def create_teams(self, run_id):
        solo_name = f"load-{run_id}-solo"
        solo_id = self.create("/teams", "POST /teams", {"name": solo_name})

        self.request(
            "GET", f"/teams/{solo_id}", "GET /teams/{teamId}",
            validate=self.object_validator({"id": solo_id, "name": solo_name}),
        )

        renamed = f"{solo_name}-renamed"
        self.request(
            "PUT", f"/teams/{solo_id}", "PUT /teams/{teamId}",
            payload={"name": renamed},
            validate=self.object_validator({"id": solo_id, "name": renamed}),
        )
        self.request(
            "GET", f"/teams/{solo_id}", "GET /teams/{teamId}",
            validate=self.object_validator({"id": solo_id, "name": renamed}),
        )
        self.request(
            "GET", "/teams", "GET /teams", validate=self.list_contains([solo_id]),
        )
        self.request(
            "DELETE", f"/teams/{solo_id}", "DELETE /teams/{teamId}", status=204,
        )

        # Exercise the bulk-generation endpoint, then remove the generated team.
        generated = self.request(
            "POST", "/teams/generate", "POST /teams/generate", 200,
            payload={"count": 1}, validate=self.generated_teams_validator,
        )
        generated_id = generated[0]["id"]
        self.request(
            "GET", f"/teams/{generated_id}", "GET /teams/{teamId}",
            validate=self.object_validator(
                {"id": generated_id, "name": generated[0]["name"]}
            ),
        )
        self.request(
            "DELETE", f"/teams/{generated_id}", "DELETE /teams/{teamId}", status=204,
        )

        team_ids = []
        team_names = []
        for index in range(self.team_count):
            name = f"load-{run_id}-team-{index}"
            team_ids.append(self.create("/teams", "POST /teams", {"name": name}))
            team_names.append(name)

        self.request(
            "GET", "/teams", "GET /teams", validate=self.list_contains(team_ids),
        )
        self.request(
            "GET", f"/teams/{team_ids[0]}", "GET /teams/{teamId}",
            validate=self.object_validator(
                {"id": team_ids[0], "name": team_names[0]}
            ),
        )
        return team_ids

    def run_tournament(self, run_id, team_ids):
        tournament_name = f"load-{run_id}-tournament"
        tournament_format = {
            "type": self.tournament_type,
            "numberOfGroups": 1,
            "maxTeamsPerGroup": len(team_ids),
        }

        tournament_id = self.create(
            "/tournaments", "POST /tournaments",
            {"name": tournament_name, "format": tournament_format},
        )
        self.request(
            "GET", f"/tournaments/{tournament_id}",
            "GET /tournaments/{tournamentId}",
            validate=self.object_validator(
                {"id": tournament_id, "name": tournament_name, "status": "NOT_STARTED"}
            ),
        )
        self.request(
            "GET", "/tournaments", "GET /tournaments",
            validate=self.list_contains([tournament_id]),
        )

        renamed = f"{tournament_name}-updated"
        self.request(
            "PUT", f"/tournaments/{tournament_id}",
            "PUT /tournaments/{tournamentId}",
            payload={"name": renamed, "format": tournament_format},
            validate=self.object_validator({"id": tournament_id, "name": renamed}),
        )

        patched = f"{renamed}-patched"
        self.request(
            "PATCH", f"/tournaments/{tournament_id}",
            "PATCH /tournaments/{tournamentId}",
            payload={"name": patched},
            validate=self.object_validator({"id": tournament_id, "name": patched}),
        )

        group_path = f"/tournaments/{tournament_id}/groups"
        group_name = f"load-{run_id}-group"
        group_id = self.create(
            group_path, "POST /tournaments/{tournamentId}/groups",
            {"name": group_name},
        )

        self.request(
            "GET", group_path, "GET /tournaments/{tournamentId}/groups",
            validate=self.list_contains([group_id]),
        )
        self.request(
            "GET", f"{group_path}/{group_id}",
            "GET /tournaments/{tournamentId}/groups/{groupId}",
            validate=self.object_validator({"id": group_id, "name": group_name}),
        )

        renamed_group = f"{group_name}-renamed"
        self.request(
            "PUT", f"{group_path}/{group_id}",
            "PUT /tournaments/{tournamentId}/groups/{groupId}",
            payload={"name": renamed_group},
            validate=self.object_validator({"id": group_id, "name": renamed_group}),
        )

        self.request(
            "PATCH", f"{group_path}/{group_id}/teams",
            "PATCH /tournaments/{tournamentId}/groups/{groupId}/teams",
            status=204, payload={"teamIds": team_ids},
        )
        self.request(
            "GET", f"{group_path}/{group_id}",
            "GET /tournaments/{tournamentId}/groups/{groupId}",
            validate=self.group_with_teams(group_id, renamed_group, team_ids),
        )

        expected_matches = len(team_ids) * (len(team_ids) - 1) // 2
        matches = self.request(
            "POST", f"{group_path}/{group_id}/generate-matches",
            "POST /tournaments/{tournamentId}/groups/{groupId}/generate-matches",
            201, validate=self.match_list_validator(expected_matches, "GROUP"),
        )

        matches_path = f"/tournaments/{tournament_id}/matches"
        self.request(
            "GET", matches_path, "GET /tournaments/{tournamentId}/matches",
            validate=self.match_list_validator(expected_matches, "GROUP"),
        )

        match_id = matches[0]["id"]
        self.request(
            "GET", f"{matches_path}/{match_id}",
            "GET /tournaments/{tournamentId}/matches/{matchId}",
            validate=self.object_validator(
                {"id": match_id, "stage": "GROUP", "round": 1}
            ),
        )
        self.request(
            "PATCH", f"{matches_path}/{match_id}/score",
            "PATCH /tournaments/{tournamentId}/matches/{matchId}/score",
            payload={"homeTeamScore": 3, "visitorTeamScore": 0},
            validate=self.object_validator(
                {
                    "id": match_id,
                    "score": {"homeTeamScore": 3, "visitorTeamScore": 0},
                    "winner": "HOME",
                }
            ),
        )

        simulated_matches = self.request(
            "POST", f"{matches_path}/simulate-results",
            "POST /tournaments/{tournamentId}/matches/simulate-results",
            payload={"maxGoals": 5}, validate=self.simulated_matches_validator,
            timeout=120,
        )
        expected_recent_count = min(
            20, sum(match.get("completedAt") is not None for match in simulated_matches)
        )
        self.request(
            "GET", f"{matches_path}/recent",
            "GET /tournaments/{tournamentId}/matches/recent",
            validate=lambda response: self.recent_matches_validator(
                response, expected_recent_count
            ),
        )
        self.request(
            "GET", f"/tournaments/{tournament_id}",
            "GET /tournaments/{tournamentId}",
            validate=self.finished_tournament(tournament_id, patched, team_ids),
        )
        return tournament_id

    def run_explicit_match(self, run_id, team_ids):
        """Exercise the explicit match API, then delete its tournament."""
        tournament_id = self.create(
            "/tournaments", "POST /tournaments",
            {
                "name": f"load-{run_id}-explicit-match",
                "format": {
                    "type": "ROUND_ROBIN",
                    "numberOfGroups": 1,
                    "maxTeamsPerGroup": 2,
                },
            },
        )
        group_path = f"/tournaments/{tournament_id}/groups"
        group_id = self.create(
            group_path, "POST /tournaments/{tournamentId}/groups",
            {"name": f"load-{run_id}-explicit-group"},
        )
        self.request(
            "PATCH", f"{group_path}/{group_id}/teams",
            "PATCH /tournaments/{tournamentId}/groups/{groupId}/teams",
            status=204, payload={"teamIds": team_ids[:2]},
        )

        matches_path = f"/tournaments/{tournament_id}/matches"
        match_id = self.create(
            matches_path, "POST /tournaments/{tournamentId}/matches",
            {
                "groupId": group_id,
                "homeTeamId": team_ids[0],
                "visitorTeamId": team_ids[1],
            },
        )
        self.request(
            "GET", f"{matches_path}/{match_id}",
            "GET /tournaments/{tournamentId}/matches/{matchId}",
            validate=self.object_validator(
                {"id": match_id, "groupId": group_id, "stage": "GROUP"}
            ),
        )
        self.request(
            "DELETE", f"{matches_path}/{match_id}",
            "DELETE /tournaments/{tournamentId}/matches/{matchId}", status=204,
        )
        self.request(
            "GET", matches_path, "GET /tournaments/{tournamentId}/matches",
            validate=self.match_list_validator(0),
        )
        self.request(
            "DELETE", f"{group_path}/{group_id}",
            "DELETE /tournaments/{tournamentId}/groups/{groupId}", status=204,
        )
        self.request(
            "DELETE", f"/tournaments/{tournament_id}",
            "DELETE /tournaments/{tournamentId}", status=204,
        )

    def cleanup(self, tournament_id, team_ids):
        self.request(
            "DELETE", f"/tournaments/{tournament_id}",
            "DELETE /tournaments/{tournamentId}", status=204,
        )
        for team_id in team_ids:
            self.request(
                "DELETE", f"/teams/{team_id}", "DELETE /teams/{teamId}",
                status=204,
            )
