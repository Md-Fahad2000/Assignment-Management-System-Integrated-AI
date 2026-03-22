
export default function StressMeter({ value, label = 'Stress', size = 'medium', showValue = true }) {
  const hasValue = value != null && typeof value === 'number' && value >= 1 && value <= 5;
  const angle = hasValue ? ((value - 1) / 4) * 180 : 90; 
  const displayValue = hasValue ? value.toFixed(1) : '—';

  const isSmall = size === 'small';
  const width = isSmall ? 120 : 220;
  const height = isSmall ? 75 : 130;
  const cx = width / 2;
  const cy = height - 8;
  const radius = isSmall ? 44 : 88;
  const strokeWidth = isSmall ? 10 : 14;

  const startAngle = 180;
  const endAngle = 0;
  const toRad = (deg) => (deg * Math.PI) / 180;
  const x = (r, a) => cx + r * Math.cos(toRad(a));
  const y = (r, a) => cy - r * Math.sin(toRad(a));
  const describeArc = (r, start, end) => {
    const large = Math.abs(end - start) > 180 ? 1 : 0;
    const sweep = start > end ? 1 : 0;
    return `M ${x(r, start)} ${y(r, start)} A ${r} ${r} 0 ${large} ${sweep} ${x(r, end)} ${y(r, end)}`;
  };

   const segments = [
    { start: 180, end: 144, color: '#22c55e' },   
    { start: 144, end: 108, color: '#84cc16' },  
    { start: 108, end: 72, color: '#eab308' },    
    { start: 72, end: 36, color: '#f97316' },     
    { start: 36, end: 0, color: '#dc2626' },     
  ];

  const needleAngle = 180 - angle;
  const needleLength = radius - strokeWidth / 2;
  const needleX = cx + needleLength * Math.cos(toRad(needleAngle));
  const needleY = cy - needleLength * Math.sin(toRad(needleAngle));

  const labels = ['Low stress', 'Mild', 'Moderate', 'High stress', 'Extreme'];
  const labelAngles = [162, 126, 90, 54, 18]; 

  return (
    <div className={`stress-meter stress-meter--${size}`}>
      <svg viewBox={`0 0 ${width} ${height}`} className="stress-meter-svg" aria-hidden="true">
        <defs>
          <linearGradient id="stress-meter-bg" x1="0%" y1="0%" x2="100%" y2="0%">
            <stop offset="0%" stopColor="#22c55e" />
            <stop offset="25%" stopColor="#84cc16" />
            <stop offset="50%" stopColor="#eab308" />
            <stop offset="75%" stopColor="#f97316" />
            <stop offset="100%" stopColor="#dc2626" />
          </linearGradient>
        </defs>
        {/* Arc segments */}
        {segments.map((seg, i) => (
          <path
            key={i}
            d={describeArc(radius, seg.start, seg.end)}
            fill="none"
            stroke={seg.color}
            strokeWidth={strokeWidth}
            strokeLinecap="round"
          />
        ))}
        {/* Needle */}
        <line
          x1={cx}
          y1={cy}
          x2={needleX}
          y2={needleY}
          stroke={hasValue ? '#1f2937' : '#9ca3af'}
          strokeWidth={isSmall ? 2 : 3}
          strokeLinecap="round"
          className="stress-meter-needle"
        />
        {/* Center dot */}
        <circle cx={cx} cy={cy} r={isSmall ? 4 : 6} fill="#1f2937" />
      </svg>
      <div className="stress-meter-labels">
        {!isSmall && labels.map((l, i) => (
          <span key={i} className="stress-meter-label" data-segment={i + 1}>{l}</span>
        ))}
      </div>
      {showValue && (
        <p className="stress-meter-now">
          <span className="stress-meter-now-value">{displayValue}</span>
          <span className="stress-meter-now-scale">/5</span>
          {hasValue && (
            <span className="stress-meter-now-text">
              — {labels[Math.min(4, Math.max(0, Math.round(value) - 1))]}
            </span>
          )}
        </p>
      )}
      {label && <p className="stress-meter-title">{label}</p>}
    </div>
  );
}
