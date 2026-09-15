// Estado en memoria de la sesión del panel.
const state = {
  teams: [],
  tournaments: [],
  selectedTournamentId: null,
  groups: [],
  matches: [],
};

// --- Utilidades de UI ---

function showToast(message, isError = false) {
  const toast = document.getElementById("toast");
  toast.textContent = message;
  toast.classList.toggle("error", isError);
  toast.hidden = false;
  clearTimeout(showToast._t);
  showToast._t = setTimeout(() => (toast.hidden = true), 3500);
}

function switchView(view) {
  document.querySelectorAll(".nav-item").forEach((btn) => {
    btn.classList.toggle("is-active", btn.dataset.view === view);
  });
  document.querySelectorAll(".view").forEach((section) => {
    section.classList.toggle("is-active", section.id === `view-${view}`);
  });
}

async function safeCall(fn, errorPrefix) {
  try {
    return await fn();
  } catch (err) {
    showToast(`${errorPrefix}: ${err.message}`, true);
    return null;
  }
}

// --- Health check ---

async function checkApiHealth() {
  const statusEl = document.getElementById("api-status");
  statusEl.textContent = "Verificando...";
  statusEl.className = "api-status";
  try {
    await Api.checkHealth();
    statusEl.textContent = "Conectado";
    statusEl.classList.add("ok");
  } catch {
    statusEl.textContent = "Sin conexión";
    statusEl.classList.add("error");
  }
}

// --- Teams ---

async function loadTeams() {
  const teams = await safeCall(() => Api.getTeams(), "No se pudieron cargar los equipos");
  state.teams = teams || [];
  renderTeamsTable();
  populateTeamSelects();
}

function renderTeamsTable() {
  const tbody = document.querySelector("#teams-table tbody");
  const empty = document.getElementById("teams-empty");
  tbody.innerHTML = "";

  if (state.teams.length === 0) {
    empty.hidden = false;
    return;
  }
  empty.hidden = true;

  for (const team of state.teams) {
    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${escapeHtml(team.name)}</td>
      <td class="id-cell">${escapeHtml(team.id)}</td>
      <td><button class="btn btn-danger btn-small" data-action="delete-team" data-id="${team.id}">Eliminar</button></td>
    `;
    tbody.appendChild(tr);
  }
}

async function handleCreateTeam(e) {
  e.preventDefault();
  const nameInput = document.getElementById("team-name");
  const name = nameInput.value.trim();
  if (!name) return;

  const created = await safeCall(() => Api.createTeam({ name }), "No se pudo crear el equipo");
  if (created) {
    showToast(`Equipo "${name}" creado`);
    nameInput.value = "";
    await loadTeams();
  }
}

async function handleDeleteTeam(id) {
  const ok = await safeCall(() => Api.deleteTeam(id), "No se pudo eliminar el equipo");
  if (ok !== null || true) {
    // DELETE exitoso devuelve null (204); si hubo error, safeCall ya mostró el toast.
    await loadTeams();
  }
}

// --- Tournaments ---

async function loadTournaments() {
  const tournaments = await safeCall(() => Api.getTournaments(), "No se pudieron cargar los torneos");
  state.tournaments = tournaments || [];
  renderTournamentsTable();
  populateTournamentSelects();
}

function renderTournamentsTable() {
  const tbody = document.querySelector("#tournaments-table tbody");
  const empty = document.getElementById("tournaments-empty");
  tbody.innerHTML = "";

  if (state.tournaments.length === 0) {
    empty.hidden = false;
    return;
  }
  empty.hidden = true;

  for (const t of state.tournaments) {
    const statusLabels = {
      NOT_STARTED: "No iniciado",
      IN_PROGRESS: "En progreso",
      FINISHED: "Finalizado",
    };
    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${escapeHtml(t.name)}</td>
      <td>${t.format?.type ?? "-"}</td>
      <td>${statusLabels[t.status] ?? t.status ?? "-"}</td>
      <td class="num">${t.groups?.length ?? 0}</td>
      <td class="num">${t.matches?.length ?? 0}</td>
      <td><button class="btn btn-danger btn-small" data-action="delete-tournament" data-id="${t.id}">Eliminar</button></td>
    `;
    tbody.appendChild(tr);
  }
}

async function handleCreateTournament(e) {
  e.preventDefault();
  const name = document.getElementById("t-name").value.trim();
  const type = document.getElementById("t-type").value;
  const numberOfGroups = parseInt(document.getElementById("t-groups").value, 10);
  const maxTeamsPerGroup = parseInt(document.getElementById("t-max-teams").value, 10);
  if (!name) return;

  const dto = { name, format: { type, numberOfGroups, maxTeamsPerGroup } };
  const created = await safeCall(() => Api.createTournament(dto), "No se pudo crear el torneo");
  if (created) {
    showToast(`Torneo "${name}" creado`);
    document.getElementById("t-name").value = "";
    await loadTournaments();
  }
}

async function handleDeleteTournament(id) {
  await safeCall(() => Api.deleteTournament(id), "No se pudo eliminar el torneo");
  await loadTournaments();
  if (state.selectedTournamentId === id) {
    state.selectedTournamentId = null;
    state.groups = [];
    state.matches = [];
    renderGroups();
    renderMatchesTable();
  }
}

function populateTournamentSelects() {
  const selects = [
    document.getElementById("group-tournament-select"),
    document.getElementById("match-tournament-select"),
  ];

  for (const select of selects) {
    const previous = select.value;
    select.innerHTML = state.tournaments
      .map((t) => `<option value="${t.id}">${escapeHtml(t.name)}</option>`)
      .join("");
    if (previous && state.tournaments.some((t) => t.id === previous)) {
      select.value = previous;
    }
  }

  if (!state.selectedTournamentId && state.tournaments.length > 0) {
    state.selectedTournamentId = state.tournaments[0].id;
    selects.forEach((s) => (s.value = state.selectedTournamentId));
  }
}

// --- Groups ---

async function loadGroups() {
  if (!state.selectedTournamentId) {
    state.groups = [];
    renderGroups();
    return;
  }
  const groups = await safeCall(
    () => Api.getGroups(state.selectedTournamentId),
    "No se pudieron cargar los grupos"
  );
  state.groups = groups || [];
  renderGroups();
  populateMatchGroupSelect();
}

function renderGroups() {
  const container = document.getElementById("groups-list");
  container.innerHTML = "";

  if (!state.selectedTournamentId) {
    container.innerHTML = `<p class="empty-state">Selecciona un torneo para ver sus grupos.</p>`;
    return;
  }

  if (state.groups.length === 0) {
    container.innerHTML = `<p class="empty-state">Este torneo no tiene grupos todavía.</p>`;
    return;
  }

  for (const group of state.groups) {
    const assignedIds = new Set((group.teams || []).map((t) => t.id));
    const availableTeams = state.teams.filter((t) => !assignedIds.has(t.id));

    const card = document.createElement("div");
    card.className = "group-card";
    card.innerHTML = `
      <h3>${escapeHtml(group.name)}</h3>
      <div class="team-chip-row">
        ${
          (group.teams || []).length > 0
            ? group.teams.map((t) => `<span class="team-chip">${escapeHtml(t.name)}</span>`).join("")
            : `<span class="empty-state">Sin equipos asignados</span>`
        }
      </div>
      <div class="assign-row">
        <select data-group-id="${group.id}" class="assign-select">
          ${availableTeams.map((t) => `<option value="${t.id}">${escapeHtml(t.name)}</option>`).join("")}
        </select>
        <button class="btn btn-secondary btn-small" data-action="assign-team" data-group-id="${group.id}">
          Asignar
        </button>
        <button class="btn btn-danger btn-small" data-action="delete-group" data-group-id="${group.id}">
          Eliminar grupo
        </button>
      </div>
      <div class="assign-row" style="margin-top: 10px;">
        <button class="btn btn-primary btn-small" data-action="generate-matches" data-group-id="${group.id}"
          ${(group.teams || []).length < 2 ? "disabled" : ""}>
          Generar calendario Round Robin
        </button>
        ${
          (group.teams || []).length < 2
            ? `<span class="empty-state">Necesitas al menos 2 equipos asignados</span>`
            : ""
        }
      </div>
    `;
    container.appendChild(card);
  }
}

async function handleCreateGroup(e) {
  e.preventDefault();
  if (!state.selectedTournamentId) {
    showToast("Selecciona un torneo primero", true);
    return;
  }
  const nameInput = document.getElementById("group-name");
  const name = nameInput.value.trim();
  if (!name) return;

  const created = await safeCall(
    () => Api.createGroup(state.selectedTournamentId, { name }),
    "No se pudo crear el grupo"
  );
  if (created) {
    showToast(`Grupo "${name}" creado`);
    nameInput.value = "";
    await loadGroups();
  }
}

async function handleAssignTeam(groupId) {
  const select = document.querySelector(`.assign-select[data-group-id="${groupId}"]`);
  const teamId = select?.value;
  if (!teamId) {
    showToast("No hay equipos disponibles para asignar", true);
    return;
  }

  const group = state.groups.find((g) => g.id === groupId);
  const existingIds = (group?.teams || []).map((t) => t.id);
  const teamIds = [...existingIds, teamId];

  const ok = await safeCall(
    () => Api.assignTeams(state.selectedTournamentId, groupId, teamIds),
    "No se pudo asignar el equipo"
  );
  if (ok !== undefined) {
    showToast("Equipo asignado al grupo");
    await loadGroups();
  }
}

async function handleDeleteGroup(groupId) {
  await safeCall(
    () => Api.deleteGroup(state.selectedTournamentId, groupId),
    "No se pudo eliminar el grupo"
  );
  await loadGroups();
}

async function handleGenerateMatches(groupId) {
  const ok = await safeCall(
    () => Api.generateRoundRobinMatches(state.selectedTournamentId, groupId),
    "No se pudo generar el calendario"
  );
  if (ok) {
    showToast(`Calendario generado: ${ok.length} partidos creados`);
    await loadTournaments();
    switchView("matches");
    document.getElementById("match-tournament-select").value = state.selectedTournamentId;
    await loadMatches();
  }
}

// --- Matches ---

async function loadMatches() {
  if (!state.selectedTournamentId) {
    state.matches = [];
    renderMatchesTable();
    return;
  }
  const matches = await safeCall(
    () => Api.getMatches(state.selectedTournamentId),
    "No se pudieron cargar los partidos"
  );
  state.matches = matches || [];
  renderMatchesTable();
}

function teamName(teamId) {
  const team = state.teams.find((t) => t.id === teamId);
  return team ? team.name : teamId;
}

function renderMatchesTable() {
  const tbody = document.querySelector("#matches-table tbody");
  const empty = document.getElementById("matches-empty");
  tbody.innerHTML = "";

  if (!state.selectedTournamentId || state.matches.length === 0) {
    empty.hidden = false;
    return;
  }
  empty.hidden = true;

  for (const match of state.matches) {
    const homeName = teamName(match.homeTeamId);
    const visitorName = teamName(match.visitorTeamId);
    const homeScore = match.score?.homeTeamScore ?? 0;
    const visitorScore = match.score?.visitorTeamScore ?? 0;

    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td class="${match.winner === "HOME" ? "winner-cell" : ""}">${escapeHtml(homeName)}</td>
      <td class="num">
        <div class="score-input-group">
          <input type="number" min="0" value="${homeScore}" data-role="home-score" data-match-id="${match.id}" />
          <span>–</span>
          <input type="number" min="0" value="${visitorScore}" data-role="visitor-score" data-match-id="${match.id}" />
        </div>
      </td>
      <td>${escapeHtml(visitorName)}</td>
      <td>
        <span class="score-badge ${match.winner ? "winner" : ""}">
          ${match.winner ? (match.winner === "HOME" ? homeName : visitorName) : "—"}
        </span>
      </td>
      <td>
        <button class="btn btn-secondary btn-small" data-action="save-score" data-match-id="${match.id}">Guardar</button>
        <button class="btn btn-danger btn-small" data-action="delete-match" data-match-id="${match.id}">Eliminar</button>
      </td>
    `;
    tbody.appendChild(tr);
  }
}

function populateMatchGroupSelect() {
  const select = document.getElementById("match-group");
  select.innerHTML =
    `<option value="">Sin grupo</option>` +
    state.groups.map((g) => `<option value="${g.id}">${escapeHtml(g.name)}</option>`).join("");
}

function populateTeamSelects() {
  const homeSelect = document.getElementById("match-home");
  const visitorSelect = document.getElementById("match-visitor");
  const options = state.teams.map((t) => `<option value="${t.id}">${escapeHtml(t.name)}</option>`).join("");
  homeSelect.innerHTML = options;
  visitorSelect.innerHTML = options;
}

async function handleCreateMatch(e) {
  e.preventDefault();
  if (!state.selectedTournamentId) {
    showToast("Selecciona un torneo primero", true);
    return;
  }

  const groupId = document.getElementById("match-group").value || null;
  const homeTeamId = document.getElementById("match-home").value;
  const visitorTeamId = document.getElementById("match-visitor").value;

  if (!homeTeamId || !visitorTeamId) {
    showToast("Selecciona equipo local y visitante", true);
    return;
  }
  if (homeTeamId === visitorTeamId) {
    showToast("El equipo local y visitante no pueden ser el mismo", true);
    return;
  }

  const dto = { groupId, homeTeamId, visitorTeamId };
  const created = await safeCall(
    () => Api.createMatch(state.selectedTournamentId, dto),
    "No se pudo crear el partido"
  );
  if (created) {
    showToast("Partido creado");
    await loadMatches();
    await loadTournaments();
  }
}

async function handleSaveScore(matchId) {
  const homeInput = document.querySelector(`input[data-role="home-score"][data-match-id="${matchId}"]`);
  const visitorInput = document.querySelector(`input[data-role="visitor-score"][data-match-id="${matchId}"]`);
  const homeTeamScore = parseInt(homeInput.value, 10);
  const visitorTeamScore = parseInt(visitorInput.value, 10);

  if (Number.isNaN(homeTeamScore) || Number.isNaN(visitorTeamScore) || homeTeamScore < 0 || visitorTeamScore < 0) {
    showToast("Los marcadores deben ser números enteros no negativos", true);
    return;
  }

  const ok = await safeCall(
    () => Api.updateScore(state.selectedTournamentId, matchId, { homeTeamScore, visitorTeamScore }),
    "No se pudo actualizar el marcador"
  );
  if (ok) {
    showToast("Marcador actualizado");
    await loadMatches();
    await loadTournaments();
  }
}

async function handleDeleteMatch(matchId) {
  await safeCall(
    () => Api.deleteMatch(state.selectedTournamentId, matchId),
    "No se pudo eliminar el partido"
  );
  await loadMatches();
  await loadTournaments();
}

// --- Helpers ---

function escapeHtml(str) {
  const div = document.createElement("div");
  div.textContent = str ?? "";
  return div.innerHTML;
}

// --- Wiring de eventos ---

function initNav() {
  document.querySelectorAll(".nav-item").forEach((btn) => {
    btn.addEventListener("click", () => switchView(btn.dataset.view));
  });
}

function initApiConfig() {
  document.getElementById("api-base").addEventListener("change", checkApiHealth);
}

function initForms() {
  document.getElementById("form-team").addEventListener("submit", handleCreateTeam);
  document.getElementById("form-tournament").addEventListener("submit", handleCreateTournament);
  document.getElementById("form-group").addEventListener("submit", handleCreateGroup);
  document.getElementById("form-match").addEventListener("submit", handleCreateMatch);
}

function initTournamentSelects() {
  const groupSelect = document.getElementById("group-tournament-select");
  const matchSelect = document.getElementById("match-tournament-select");

  const onChange = async (e) => {
    state.selectedTournamentId = e.target.value || null;
    groupSelect.value = state.selectedTournamentId ?? "";
    matchSelect.value = state.selectedTournamentId ?? "";
    await Promise.all([loadGroups(), loadMatches()]);
  };

  groupSelect.addEventListener("change", onChange);
  matchSelect.addEventListener("change", onChange);
}

function initDelegatedClicks() {
  document.body.addEventListener("click", (e) => {
    const target = e.target.closest("[data-action]");
    if (!target) return;

    const action = target.dataset.action;
    switch (action) {
      case "delete-team":
        handleDeleteTeam(target.dataset.id);
        break;
      case "delete-tournament":
        handleDeleteTournament(target.dataset.id);
        break;
      case "assign-team":
        handleAssignTeam(target.dataset.groupId);
        break;
      case "delete-group":
        handleDeleteGroup(target.dataset.groupId);
        break;
      case "generate-matches":
        handleGenerateMatches(target.dataset.groupId);
        break;
      case "save-score":
        handleSaveScore(target.dataset.matchId);
        break;
      case "delete-match":
        handleDeleteMatch(target.dataset.matchId);
        break;
    }
  });
}

async function init() {
  initNav();
  initApiConfig();
  initForms();
  initTournamentSelects();
  initDelegatedClicks();

  await checkApiHealth();
  await loadTeams();
  await loadTournaments();
  await loadGroups();
  await loadMatches();
}

document.addEventListener("DOMContentLoaded", init);
