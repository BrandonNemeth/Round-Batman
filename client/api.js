// Cliente HTTP para Round-Batman API.

const Api = (() => {
  function baseUrl() {
    return document.getElementById("api-base").value.replace(/\/$/, "");
  }

  async function request(method, path, body) {
    const url = `${baseUrl()}${path}`;
    const res = await fetch(url, {
      method,
      headers: body ? { "Content-Type": "application/json" } : undefined,
      body: body ? JSON.stringify(body) : undefined,
    });

    if (res.status === 204) return null;

    let data = null;
    try {
      data = await res.json();
    } catch {
      // Sin cuerpo o no-JSON; se ignora.
    }

    if (!res.ok) {
      const message = data?.title || data?.detail || `${res.status} ${res.statusText}`;
      throw new Error(message);
    }

    return data;
  }

  async function checkHealth() {
    const res = await fetch(`${baseUrl()}/health`);
    if (!res.ok) throw new Error("Health check failed");
    return res.text();
  }

  return {
    checkHealth,

    // Teams
    getTeams: () => request("GET", "/teams"),
    createTeam: (dto) => request("POST", "/teams", dto),
    deleteTeam: (id) => request("DELETE", `/teams/${id}`),

    // Tournaments
    getTournaments: () => request("GET", "/tournaments"),
    getTournament: (id) => request("GET", `/tournaments/${id}`),
    createTournament: (dto) => request("POST", "/tournaments", dto),
    deleteTournament: (id) => request("DELETE", `/tournaments/${id}`),

    // Groups
    getGroups: (tournamentId) => request("GET", `/tournaments/${tournamentId}/groups`),
    createGroup: (tournamentId, dto) => request("POST", `/tournaments/${tournamentId}/groups`, dto),
    deleteGroup: (tournamentId, groupId) => request("DELETE", `/tournaments/${tournamentId}/groups/${groupId}`),
    assignTeams: (tournamentId, groupId, teamIds) =>
      request("PATCH", `/tournaments/${tournamentId}/groups/${groupId}/teams`, { teamIds }),

    // Matches
    getMatches: (tournamentId) => request("GET", `/tournaments/${tournamentId}/matches`),
    createMatch: (tournamentId, dto) => request("POST", `/tournaments/${tournamentId}/matches`, dto),
    updateScore: (tournamentId, matchId, dto) =>
      request("PATCH", `/tournaments/${tournamentId}/matches/${matchId}/score`, dto),
    deleteMatch: (tournamentId, matchId) => request("DELETE", `/tournaments/${tournamentId}/matches/${matchId}`),
  };
})();
