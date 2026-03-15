import { useEffect, useState, useRef } from 'react';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import { assignmentsApi } from '../api/client';
import './AssignmentAssistant.css';

export default function AssignmentAssistant() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const explainStep = searchParams.get('explainStep');
  const stepTitle = searchParams.get('stepTitle');
  const focusNow = searchParams.get('focusNow');
  const [assignment, setAssignment] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [messages, setMessages] = useState([]);
  const [input, setInput] = useState('');
  const [sending, setSending] = useState(false);
  const explainSentRef = useRef(false);
  const focusNowSentRef = useRef(false);

  const QUICK_PROMPTS = [
    'Create a full roadmap for this assignment.',
    'What should I do today?',
    "I have 2 hours today – what should I do from the roadmap?",
    'Plan my day: what should I do today from the roadmap and routine?',
    'Summarize this assignment in simple steps.',
    'How can I finish before the deadline?',
    'What should I focus on right now?',
  ];

  const lastMessage = messages.length > 0 ? messages[messages.length - 1] : null;
  const [followUpPrompts, setFollowUpPrompts] = useState([]);
  const [loadingFollowUps, setLoadingFollowUps] = useState(false);
  const showFollowUps = !sending && lastMessage?.role === 'assistant' && messages.length > 1;

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setError('');
      try {
        const [a, history] = await Promise.all([
          assignmentsApi.get(id),
          assignmentsApi.getAssistantHistory(id),
        ]);
        if (!cancelled) {
          setAssignment(a);
          const list = history?.messages && Array.isArray(history.messages) ? history.messages : [];
          setMessages(
            list.length > 0
              ? list.map((m) => ({ ...m, id: m.id }))
              : [
                  {
                    role: 'assistant',
                    content:
                      'I am your AI assistant for this assignment. Ask anything about the requirements, how to start, how to plan your work according to your routine and deadline, or how to use the AI roadmap.',
                  },
                ]
          );
          setFollowUpPrompts([]);
        }
      } catch (err) {
        if (!cancelled) setError(err.message || 'Failed to load assignment.');
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [id]);

  useEffect(() => {
    if (!assignment || loading || sending || !explainStep || explainSentRef.current) return;
    const text = stepTitle
      ? `Explain step ${explainStep} ("${decodeURIComponent(stepTitle)}") in simple terms.`
      : `Explain step ${explainStep} of the roadmap in simple terms.`;
    explainSentRef.current = true;
    setSearchParams({});
    sendMessage(text);
  }, [assignment, loading, explainStep, stepTitle]);

  useEffect(() => {
    if (!assignment || loading || sending || !focusNow || focusNowSentRef.current) return;
    focusNowSentRef.current = true;
    setSearchParams({});
    sendMessage('What should I focus on right now?');
  }, [assignment, loading, focusNow]);

  const sendMessage = async (text) => {
    const t = (text || input?.trim() || '').trim();
    if (!t || !assignment?.id || sending) return;
    const next = [...messages, { role: 'user', content: t }];
    setMessages(next);
    setInput('');
    setFollowUpPrompts([]);
    setSending(true);
    try {
      const res = await assignmentsApi.askAssistant(assignment.id, t);
      const assistantContent = res?.answer || 'No answer.';
      setMessages([...next, { role: 'assistant', content: assistantContent, id: res?.assistantMessageId }]);
      if (assistantContent && assistantContent.length > 50) {
        setLoadingFollowUps(true);
        try {
          const fu = await assignmentsApi.suggestFollowUps(assignment.id, assistantContent);
          if (Array.isArray(fu?.followUps) && fu.followUps.length > 0) setFollowUpPrompts(fu.followUps);
          else setFollowUpPrompts(['What should I do next?', 'Break this into smaller tasks.', 'What if I miss the deadline?']);
        } catch {
          setFollowUpPrompts(['What should I do next?', 'Break this into smaller tasks.', 'What if I miss the deadline?']);
        } finally {
          setLoadingFollowUps(false);
        }
      } else {
        setFollowUpPrompts(['What should I do next?', 'Break this into smaller tasks.', 'What if I miss the deadline?']);
      }
    } catch (err) {
      setMessages([...next, { role: 'assistant', content: err.message || 'Failed to get AI response.' }]);
      setFollowUpPrompts(['What should I do next?', 'Break this into smaller tasks.']);
    } finally {
      setSending(false);
    }
  };

  const copyMessage = (content) => {
    navigator.clipboard.writeText(content).then(() => {});
  };

  const rateMessage = async (messageId, rating) => {
    if (!assignment?.id || !messageId) return;
    try {
      await assignmentsApi.rateMessage(assignment.id, messageId, rating);
    } catch (_) {}
  };

  const startVoiceInput = () => {
    if (!('webkitSpeechRecognition' in window) && !('SpeechRecognition' in window)) return;
    const Recognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    const rec = new Recognition();
    rec.continuous = false;
    rec.interimResults = false;
    rec.lang = 'en-US';
    rec.onresult = (e) => {
      const t = e.results[0][0].transcript;
      if (t) setInput((prev) => (prev ? prev + ' ' + t : t));
    };
    rec.start();
  };

  const handleSend = (e) => {
    e?.preventDefault();
    sendMessage(input);
  };

  return (
    <div className="assignment-assistant-page">
      <div className="assistant-header">
        <nav className="assistant-breadcrumb" aria-label="Breadcrumb">
          <button type="button" className="breadcrumb-link" onClick={() => navigate('/assignments')}>Assignments</button>
          <span className="breadcrumb-sep">/</span>
          {assignment && <span className="breadcrumb-current">{assignment.title} – AI Assistant</span>}
        </nav>
        <div className="assistant-header-actions">
          <button type="button" className="btn btn-small btn-secondary" onClick={() => {
            const text = messages.map(m => `${m.role === 'user' ? 'You' : 'AI'}: ${m.content}`).join('\n\n');
            const blob = new Blob([`Assignment: ${assignment?.title ?? ''}\n\n${text}`], { type: 'text/plain' });
            const a = document.createElement('a');
            a.href = URL.createObjectURL(blob);
            a.download = `assistant-chat-${assignment?.title?.replace(/\s+/g, '-') ?? id}.txt`;
            a.click();
            URL.revokeObjectURL(a.href);
          }} disabled={messages.length <= 1}>
            Export chat
          </button>
          <button type="button" className="btn btn-secondary" onClick={() => navigate('/assignments')}>
            ← Back to Assignments
          </button>
        </div>
        <div className="assistant-title">
          <h1>Assignment AI Assistant</h1>
          {assignment && (
            <p className="assistant-subtitle">
              {assignment.title} &middot; Due {assignment.due_date?.slice(0, 10)}
            </p>
          )}
        </div>
      </div>
      {error && <p className="error-msg">{error}</p>}
      {loading ? (
        <p className="muted">Loading assignment...</p>
      ) : (
        <>
          <div className="assistant-context-note">
            AI is using your routine, this assignment, and its deadline to answer. Ask anything about planning or the roadmap.
          </div>
          {assignment?.description && (
            <div className="assistant-context">
              <h2>Assignment overview</h2>
              <p>{assignment.description}</p>
            </div>
          )}
          <div className="assistant-chat">
            <div className="assistant-messages">
              {messages.map((m, i) => (
                <div key={m.id ?? i} className={`assistant-message assistant-message-${m.role}`}>
                  <div className="assistant-message-label">
                    {m.role === 'user' ? 'You' : 'AI Assistant'}
                  </div>
                  <div className="assistant-message-bubble">
                    {m.role === 'assistant' ? (
                      <div className="assistant-reply-content">
                        {m.content
                          .split(/\n\n+/)
                          .filter((para) => para.trim())
                          .map((para, pi) => (
                            <p key={pi}>
                              {para.split('\n').map((line, li) => (
                                <span key={li}>
                                  {line}
                                  {li < para.split('\n').length - 1 && <br />}
                                </span>
                              ))}
                            </p>
                          ))}
                        {!m.content.trim() && <p className="assistant-reply-empty">No response.</p>}
                      </div>
                    ) : (
                      m.content
                    )}
                    <div className="message-actions">
                      <button type="button" className="btn-icon" onClick={() => copyMessage(m.content)} title="Copy">📋</button>
                      {m.role === 'assistant' && m.id && (
                        <>
                          <button type="button" className="btn-icon" onClick={() => rateMessage(m.id, 1)} title="Good">👍</button>
                          <button type="button" className="btn-icon" onClick={() => rateMessage(m.id, -1)} title="Bad">👎</button>
                        </>
                      )}
                    </div>
                  </div>
                </div>
              ))}
              {sending && (
                <div className="assistant-message assistant-message-assistant">
                  <div className="assistant-message-label">AI Assistant</div>
                  <div className="assistant-message-bubble assistant-typing">
                    <span className="typing-dot" /><span className="typing-dot" /><span className="typing-dot" />
                  </div>
                </div>
              )}
              {showFollowUps && (followUpPrompts.length > 0 || loadingFollowUps) && (
                <div className="assistant-follow-ups">
                  <span className="follow-ups-label">Suggested follow-ups:</span>
                  {loadingFollowUps ? <span className="muted small">Loading...</span> : (followUpPrompts.length ? followUpPrompts : ['What should I do next?', 'Break this into smaller tasks.', 'What if I miss the deadline?']).map((prompt, i) => (
                    <button key={i} type="button" className="quick-prompt-btn" onClick={() => sendMessage(prompt)}>
                      {prompt}
                    </button>
                  ))}
                </div>
              )}
            </div>
            <div className="assistant-quick-prompts">
              {QUICK_PROMPTS.map((prompt, i) => (
                <button key={i} type="button" className="quick-prompt-btn" onClick={() => sendMessage(prompt)} disabled={sending}>
                  {prompt}
                </button>
              ))}
            </div>
            <form className="assistant-input-row" onSubmit={handleSend}>
              <button type="button" className="btn btn-icon voice-btn" onClick={startVoiceInput} title="Voice input" aria-label="Voice input">
                🎤
              </button>
              <input
                type="text"
                placeholder="Ask AI about this assignment, your routine, or the roadmap..."
                value={input}
                onChange={(e) => setInput(e.target.value)}
                disabled={sending}
              />
              <button type="submit" className="btn btn-primary" disabled={sending}>
                {sending ? 'Thinking...' : 'Send'}
              </button>
            </form>
          </div>
        </>
      )}
    </div>
  );
}

