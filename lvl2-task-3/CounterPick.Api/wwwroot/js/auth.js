const AUTH_KEY = 'cp_access_token';
const REFRESH_KEY = 'cp_refresh_token';

function getToken() { return localStorage.getItem(AUTH_KEY); }

function setTokens(access, refresh) {
  localStorage.setItem(AUTH_KEY, access);
  if (refresh) localStorage.setItem(REFRESH_KEY, refresh);
}

function clearTokens() {
  localStorage.removeItem(AUTH_KEY);
  localStorage.removeItem(REFRESH_KEY);
}

function isAuthenticated() { return !!getToken(); }

function getAuthHeaders() {
  const t = getToken();
  return t ? { 'Authorization': `Bearer ${t}`, 'Content-Type': 'application/json' } : { 'Content-Type': 'application/json' };
}

function isAdmin() {
  if (!isAuthenticated()) return false;
  try {
    const payload = JSON.parse(atob(getToken().split('.')[1]));
    const roles = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
    if (Array.isArray(roles)) return roles.includes('Admin');
    return roles === 'Admin';
  } catch { return false; }
}

function getCurrentUserId() {
  if (!isAuthenticated()) return null;
  try {
    const payload = JSON.parse(atob(getToken().split('.')[1]));
    return payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
  } catch { return null; }
}

async function tryRefresh() {
  const rt = localStorage.getItem(REFRESH_KEY);
  if (!rt) return false;
  try {
    const resp = await fetch('/auth/refresh', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: rt })
    });
    if (!resp.ok) return false;
    const data = await resp.json();
    setTokens(data.accessToken, data.refreshToken);
    return true;
  } catch { return false; }
}

async function apiFetch(path, options = {}) {
  const conf = { ...options, headers: { ...getAuthHeaders(), ...options.headers } };
  let resp = await fetch(path, conf);
  if (resp.status === 429) {
    showToast('Rate limited — slow down!', true);
    return { status: 429, rateLimited: true, ok: false };
  }
  if (resp.status === 401) {
    const refreshed = await tryRefresh();
    if (refreshed) {
      conf.headers['Authorization'] = `Bearer ${getToken()}`;
      resp = await fetch(path, conf);
      if (resp.ok) return resp;
    }
    clearTokens();
    if (window.location.pathname !== '/login.html') {
      window.location.href = '/login.html';
    }
    return null;
  }
  return resp;
}

async function apiFetchJson(path, options = {}) {
  const resp = await apiFetch(path, options);
  if (!resp) return null;
  if (resp.status === 204) return null;
  const text = await resp.text();
  return text ? JSON.parse(text) : null;
}

function showToast(msg, isError = false) {
  let container = document.querySelector('.toast-container');
  if (!container) {
    container = document.createElement('div');
    container.className = 'toast-container';
    document.body.appendChild(container);
  }
  const el = document.createElement('div');
  el.className = `toast-msg${isError ? ' error' : ''}`;
  el.textContent = msg;
  container.appendChild(el);
  setTimeout(() => el.remove(), 3000);
}

function updateNavbar() {
  const loginItem = document.getElementById('nav-login');
  const logoutItem = document.getElementById('nav-logout');
  const adminItem = document.getElementById('nav-admin');
  const userBadge = document.getElementById('nav-user');

  if (!loginItem) return;

  if (isAuthenticated()) {
    loginItem.classList.add('d-none');
    logoutItem.classList.remove('d-none');
    if (adminItem) adminItem.classList.toggle('d-none', !isAdmin());
    if (userBadge) {
      try {
        const payload = JSON.parse(atob(getToken().split('.')[1]));
        userBadge.textContent = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || '';
        userBadge.classList.remove('d-none');
      } catch { userBadge.classList.add('d-none'); }
    }
  } else {
    loginItem.classList.remove('d-none');
    logoutItem.classList.add('d-none');
    if (adminItem) adminItem.classList.add('d-none');
    if (userBadge) userBadge.classList.add('d-none');
  }
}

function initNavbar() {
  document.addEventListener('DOMContentLoaded', () => {
    updateNavbar();
    const logoutBtn = document.getElementById('btn-logout');
    if (logoutBtn) logoutBtn.addEventListener('click', (e) => {
      e.preventDefault();
      clearTokens();
      updateNavbar();
      window.location.href = '/login.html';
    });
  });
}

initNavbar();
