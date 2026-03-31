const API_BASE = '/api';

function getToken() {
  return localStorage.getItem('token');
}

function getAuthHeaders() {
  const token = getToken();
  return {
    'Content-Type': 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };
}

export async function api(url, options = {}) {
  const res = await fetch(`${API_BASE}${url}`, {
    ...options,
    headers: { ...getAuthHeaders(), ...options.headers },
  });
  const data = res.status === 204 ? {} : await res.json().catch(() => ({}));
  if (!res.ok) throw new Error(data.message || res.statusText || 'Request failed');
  return data;
}

export const authApi = {
  register: (body) => api('/auth/register', { method: 'POST', body: JSON.stringify(body) }),
  verifyEmail: (body) => api('/auth/verify-email', { method: 'POST', body: JSON.stringify(body) }),
  resendVerification: (email) => api('/auth/resend-verification', { method: 'POST', body: JSON.stringify({ email }) }),
  login: (body) => api('/auth/login', { method: 'POST', body: JSON.stringify(body) }),
  forgotPassword: (email) => api('/auth/forgot-password', { method: 'POST', body: JSON.stringify({ email }) }),
  resetPassword: (body) => api('/auth/reset-password', { method: 'POST', body: JSON.stringify(body) }),
  getProfile: () => api('/auth/me'),
  updateProfile: (body) => api('/auth/profile', { method: 'PUT', body: JSON.stringify(body) }),
};

export const assignmentsApi = {
  getAll: () => api('/assignments'),
  get: (id) => api(`/assignments/${id}`),
  getAiResponses: (id) => api(`/assignments/${id}/ai-responses`),
  create: (body) => api('/assignments', { method: 'POST', body: JSON.stringify(body) }),
  update: (id, body) => api(`/assignments/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  delete: (id) => api(`/assignments/${id}`, { method: 'DELETE' }).then(() => {}),
  uploadDocument: async (id, file) => {
    const formData = new FormData();
    formData.append('file', file);
    const res = await fetch(`${API_BASE}/assignments/${id}/document`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${getToken()}` },
      body: formData,
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) throw new Error(data.message || res.statusText || 'Upload failed');
    return data;
  },
  generateRoadmap: (id) => api(`/assignments/${id}/generate-roadmap`, { method: 'POST' }),
  getAssistantHistory: (id) => api(`/assignments/${id}/assistant/history`),
  askAssistant: (id, message) => api(`/assignments/${id}/assistant`, { method: 'POST', body: JSON.stringify({ message }) }),
  summarize: (id) => api(`/assignments/${id}/summarize`),
  suggestSlots: (id, opts = {}) => api(`/assignments/${id}/suggest-slots${opts.detailed ? '?detailed=true' : ''}`),
  suggestTitle: (id) => api(`/assignments/${id}/suggest-title`),
  keyPoints: (id) => api(`/assignments/${id}/key-points`),
  simplifyRoadmap: (id) => api(`/assignments/${id}/simplify-roadmap`, { method: 'POST' }),
  setRoadmap: (id, roadmapJson) => api(`/assignments/${id}/roadmap`, { method: 'PUT', body: JSON.stringify({ roadmap_json: roadmapJson }) }),
  roadmapConflicts: (id) => api(`/assignments/${id}/roadmap-conflicts`),
  suggestFollowUps: (id, lastReply) => api(`/assignments/${id}/assistant/suggest-follow-ups`, { method: 'POST', body: JSON.stringify({ lastReply }) }),
  keyDates: (id) => api(`/assignments/${id}/key-dates`),
  quiz: (id) => api(`/assignments/${id}/quiz`),
  oneLineSummary: (id) => api(`/assignments/${id}/one-line-summary`),
  rescheduleRoadmap: (id, fromStepIndex) => api(`/assignments/${id}/reschedule-roadmap`, { method: 'POST', body: JSON.stringify({ fromStepIndex }) }),
  explainStep: (id, stepTitle, stepDescription) => api(`/assignments/${id}/explain-step?stepTitle=${encodeURIComponent(stepTitle)}&stepDescription=${encodeURIComponent(stepDescription || '')}`),
  difficultyTime: (id) => api(`/assignments/${id}/difficulty-time`),
  bestDay: (id) => api(`/assignments/${id}/best-day`),
  conflictAlternative: (id, stepIndex, conflictWith) => api(`/assignments/${id}/conflict-alternative`, { method: 'POST', body: JSON.stringify({ stepIndex, conflictWith }) }),
  rateMessage: (id, messageId, rating) => api(`/assignments/${id}/assistant/messages/${messageId}/rate`, { method: 'PUT', body: JSON.stringify({ rating }) }),
};

export const dashboardApi = {
  focusTip: () => api('/dashboard/focus-tip'),
  weeklyPlan: () => api('/dashboard/weekly-plan'),
  overloadWarning: () => api('/dashboard/overload-warning'),
  stressSummary: () => api('/dashboard/stress-summary'),
};

export const routineApi = {
  getAll: () => api('/routine'),
  getByDay: (day) => api(`/routine/day/${day}`),
  get: (id) => api(`/routine/${id}`),
  create: (body) => api('/routine', { method: 'POST', body: JSON.stringify(body) }),
  update: (id, body) => api(`/routine/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  delete: (id) => api(`/routine/${id}`, { method: 'DELETE' }).then(() => {}),
};

export const aiApi = {
  getSchedule: (from, to) => api(`/ai/schedule?from=${from}&to=${to}`),
  generateSchedule: (from, to) => api(`/ai/schedule/generate?from=${from}&to=${to}`, { method: 'POST' }),
  getRoadmap: (assignmentId) => api(`/ai/roadmap/${assignmentId}`),
  getRoadmapContext: (assignmentId) => api(`/ai/roadmap/${assignmentId}/context`),
  generateRoadmap: (assignmentId) => api(`/ai/roadmap/generate/${assignmentId}`, { method: 'POST' }),
  /** Live AI streaming: onChunk(text), onDone(steps), onError(err). Uses SSE. */
  async generateRoadmapStream(assignmentId, { onChunk, onDone, onError }) {
    const token = getToken();
    let gotDone = false;
    try {
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 95000); // ~95s, backend times out at 90s
      const res = await fetch(`${API_BASE}/ai/roadmap/generate/${assignmentId}/stream`, {
        method: 'POST',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
        signal: controller.signal,
      });
      clearTimeout(timeoutId);
      if (!res.ok) {
        const errBody = await res.text();
        let msg = res.statusText || 'Stream failed';
        try {
          const j = JSON.parse(errBody);
          if (j.message) msg = j.message;
        } catch (_) {}
        throw new Error(`${res.status}: ${msg}`);
      }
      const reader = res.body.getReader();
      const decoder = new TextDecoder();
      let buffer = '';
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        buffer += decoder.decode(value, { stream: true });
        let idx;
        while ((idx = buffer.indexOf('\n\n')) !== -1) {
          const event = buffer.slice(0, idx);
          buffer = buffer.slice(idx + 2);
          const dataLine = event.split('\n').find((l) => l.startsWith('data: '));
          if (!dataLine) continue;
          const data = dataLine.slice(6);
          if (!data || data === '[DONE]') continue;
          try {
            const obj = JSON.parse(data);
            if (obj.chunk) onChunk(obj.chunk);
            if (obj.done && Array.isArray(obj.steps)) {
              gotDone = true;
              onDone(obj.steps, obj.error || null);
            }
          } catch (_) {}
        }
      }
      if (!gotDone && buffer.trim()) {
        const dataLine = buffer.split('\n').find((l) => l.startsWith('data: '));
        if (dataLine) {
          try {
            const obj = JSON.parse(dataLine.slice(6));
            if (obj.done && Array.isArray(obj.steps)) {
              gotDone = true;
              onDone(obj.steps, obj.error || null);
            }
          } catch (_) {}
        }
      }
      if (!gotDone && onError) {
        const result = onError(new Error('Stream ended without roadmap. Use fallback.'));
        if (result && typeof result.then === 'function') await result;
      }
    } catch (err) {
      if (onError) {
        const result = onError(err);
        if (result && typeof result.then === 'function') await result;
      }
    }
  },
  priorityScore: (dueDate, priority) => api(`/ai/priority-score?dueDate=${dueDate}&priority=${priority || 'medium'}`),
};

export { getToken };
