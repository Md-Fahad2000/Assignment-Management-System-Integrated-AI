import './Help.css';

export default function Help() {
  return (
    <div className="help-page">
      <h1>Help & FAQ</h1>
      <p className="help-intro">Quick answers to get the most out of the Assignment Manager.</p>

      <section className="help-section">
        <h2>Assignments</h2>
        <h3>How do I add an assignment?</h3>
        <p>Click &quot;+ Add Assignment&quot; (or press <kbd>Ctrl+N</kbd>), enter title and due date, then Save. You can optionally add a description and attach a PDF or TXT file.</p>
        <h3>How do I get an AI roadmap?</h3>
        <p>Open an assignment with &quot;View details & roadmap&quot;, upload a PDF if you have one, then click &quot;Generate AI Roadmap&quot;. The AI uses your routine and deadline to suggest steps, excluding 10 AM–5 PM (job hours). You can click &quot;Regenerate AI Roadmap&quot; to get a new version.</p>
        <h3>Can I search or filter assignments?</h3>
        <p>Yes. Use the search box to filter by title and the status dropdown to show only Pending, In progress, or Completed.</p>
        <h3>What do the due badges mean?</h3>
        <p>&quot;Overdue&quot; = past due date. &quot;Due today&quot; and &quot;Due in X days&quot; help you prioritize. Delete is confirmed before removing an assignment.</p>
      </section>

      <section className="help-section">
        <h2>Routine</h2>
        <h3>How do I set my routine?</h3>
        <p>Go to Routine, pick a day (Sun–Sat), then &quot;+ Add Slot&quot; with a title and start/end time. Your routine is used by the AI to avoid suggesting work during busy times.</p>
        <h3>What is Week view?</h3>
        <p>Switch to &quot;Week view&quot; to see all seven days and their slots on one screen.</p>
        <h3>Can I copy slots from one day to another?</h3>
        <p>Yes. In &quot;By day&quot; view, choose &quot;Copy from...&quot; and select a day, then click &quot;Copy slots&quot; to duplicate that day&apos;s slots to the currently selected day.</p>
        <h3>What if two slots overlap?</h3>
        <p>You&apos;ll see a warning: &quot;Some slots on this day overlap.&quot; Adjust start/end times so they don&apos;t overlap. Adding a new slot that overlaps will also show a toast message.</p>
      </section>

      <section className="help-section">
        <h2>AI Assistant</h2>
        <h3>Where is the AI Assistant?</h3>
        <p>Open an assignment&apos;s details and click &quot;Open AI Assistant&quot;. You get a full-page chat that uses your assignment, routine, and deadline. Your chat is saved so you can continue later.</p>
        <h3>What can I ask?</h3>
        <p>Ask for a full roadmap, &quot;What should I do today?&quot;, a summary, or how to finish before the deadline. Use the quick prompt buttons or type your own question. The AI responds based only on your routine and this assignment.</p>
        <h3>Why does it say &quot;AI is using your routine...&quot;?</h3>
        <p>That line confirms the assistant has access to your routine and assignment so its answers are relevant and respect your busy times.</p>
      </section>

      <section className="help-section">
        <h2>Tips</h2>
        <ul>
          <li>Use <kbd>Esc</kbd> to close modals or cancel dialogs.</li>
          <li>You can drag and drop a PDF onto an assignment card or onto the file area when creating an assignment.</li>
          <li>Toasts at the bottom-right confirm saves, uploads, and errors. They disappear after a few seconds.</li>
        </ul>
      </section>
    </div>
  );
}
