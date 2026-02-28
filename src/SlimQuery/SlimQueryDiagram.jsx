import { useState } from "react";

const C = {
  bg: "#F7F6F3", surface: "#FFFFFF", surface2: "#F0EEE9",
  border: "#E2DDD6", borderDark: "#C8C2B8",
  ink: "#1A1714", inkDim: "#6B6560", inkMuted: "#A09A93",
  green: "#1A7A4A", greenBg: "#EBF7F1", greenBorder: "#A8DFC4",
  blue: "#1A4FA0", blueBg: "#EBF0FA", blueBorder: "#A8C0E8",
  amber: "#92550A", amberBg: "#FBF3E8", amberBorder: "#E8C98A",
  rose: "#9A1A2E", roseBg: "#FAE8EC", roseBorder: "#E8A8B4",
  purple: "#5A1A9A", purpleBg: "#F0EBF7", purpleBorder: "#C4A8E8",
  teal: "#0A7A7A", tealBg: "#EBF7F7", tealBorder: "#A8DFDF",
};

function Card({ children, color, bg, border, style = {}, onClick }) {
  const [hov, setHov] = useState(false);
  return (
    <div onClick={onClick} onMouseEnter={() => setHov(true)} onMouseLeave={() => setHov(false)}
      style={{
        background: hov && bg ? bg : C.surface,
        border: `1px solid ${hov ? (border || C.borderDark) : C.border}`,
        borderRadius: 10, position: "relative", overflow: "hidden",
        cursor: onClick ? "pointer" : "default", transition: "all 0.2s ease",
        transform: hov ? "translateY(-2px)" : "none",
        boxShadow: hov ? `0 6px 24px rgba(0,0,0,0.08)` : `0 1px 4px rgba(0,0,0,0.04)`,
        ...style,
      }}>
      {color && <div style={{ position: "absolute", top: 0, left: 0, right: 0, height: 3, background: color, opacity: hov ? 1 : 0.6, transition: "opacity 0.2s" }} />}
      {children}
    </div>
  );
}

function Tag({ children, color = C.blue, bg = C.blueBg, border = C.blueBorder }) {
  return (
    <span style={{ display: "inline-flex", alignItems: "center", fontSize: 10, padding: "2px 8px", borderRadius: 4, border: `1px solid ${border}`, background: bg, color, fontFamily: "monospace", whiteSpace: "nowrap", fontWeight: 600 }}>
      {children}
    </span>
  );
}

function SL({ children }) {
  return (
    <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 16 }}>
      <span style={{ fontFamily: "monospace", fontSize: 10, letterSpacing: "0.25em", textTransform: "uppercase", color: C.inkMuted, fontWeight: 600 }}>{children}</span>
      <div style={{ flex: 1, height: 1, background: C.border }} />
    </div>
  );
}

function Arrow({ label }) {
  return (
    <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 4, padding: "4px 0" }}>
      {label && <span style={{ fontFamily: "monospace", fontSize: 9, color: C.inkMuted, letterSpacing: "0.1em" }}>{label}</span>}
      <svg width={2} height={32} style={{ overflow: "visible" }}>
        <line x1={1} y1={0} x2={1} y2={28} stroke={C.borderDark} strokeWidth={1.5} strokeDasharray="4 3" />
        <polygon points="1,32 -2,26 4,26" fill={C.borderDark} />
      </svg>
    </div>
  );
}

function Philosophy() {
  const items = [
    { icon: "⚡", title: "Speed of Dapper", desc: "Compiled expression mappings. No reflection at runtime.", color: C.amber },
    { icon: "🔤", title: "SQL You Control", desc: "Raw SQL always available. No magic. No surprises.", color: C.blue },
    { icon: "🪶", title: "Zero Bloat", desc: "No DbContext. No entity tracking. No 200MB dependency tree.", color: C.green },
    { icon: "🚫", title: "No Change Tracking", desc: "Explicit updates only. No hidden state, no dirty contexts, no surprises.", color: C.rose },
  ];
  return (
    <div>
      <SL>Design Philosophy</SL>
      <div style={{ display: "grid", gridTemplateColumns: "repeat(4,1fr)", gap: 10 }}>
        {items.map(p => (
          <Card key={p.title} style={{ padding: "16px 14px", borderLeft: `3px solid ${p.color}` }}>
            <div style={{ fontSize: 22, marginBottom: 8 }}>{p.icon}</div>
            <div style={{ fontFamily: "sans-serif", fontSize: 12, fontWeight: 700, color: C.ink, marginBottom: 4 }}>{p.title}</div>
            <div style={{ fontFamily: "monospace", fontSize: 10, color: C.inkDim, lineHeight: 1.6 }}>{p.desc}</div>
          </Card>
        ))}
      </div>
    </div>
  );
}

function ApiSurface() {
  const [sel, setSel] = useState(null);
  const apis = [
    { key: "query", icon: "🔍", name: "Query Builder", color: C.blue, bg: C.blueBg, border: C.blueBorder, desc: "Fluent SQL without strings", methods: ["db.From<User>().Where(x => x.Active).ToListAsync()", "db.From<Order>().Select(x => new{x.Id,x.Total}).FirstAsync()", "db.From<T>().OrderBy(x=>x.Price).Skip(10).Take(5).ToListAsync()"] },
    { key: "raw",   icon: "⚡", name: "Raw SQL",       color: C.amber, bg: C.amberBg, border: C.amberBorder, desc: "Full control like Dapper", methods: ['db.QueryAsync<User>("SELECT * FROM users WHERE id=@Id", new{Id=1})', 'db.ExecuteAsync("UPDATE users SET active=@v WHERE id=@Id", p)', 'db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM orders")'] },
    { key: "crud",  icon: "📦", name: "CRUD Helpers",  color: C.green, bg: C.greenBg, border: C.greenBorder, desc: "Zero-SQL common operations", methods: ["db.InsertAsync(user)", "db.UpdateAsync(user)", "db.DeleteAsync<User>(id)", "db.GetByIdAsync<User>(id)"] },
    { key: "bulk",  icon: "🚀", name: "Bulk Ops",      color: C.purple, bg: C.purpleBg, border: C.purpleBorder, desc: "Native bulk — missing in Dapper", methods: ["db.BulkInsertAsync(users)", "db.BulkUpdateAsync(orders)", "db.BulkDeleteAsync<Product>(ids)"] },
  ];
  return (
    <div>
      <SL>Public API Surface</SL>
      <div style={{ display: "grid", gridTemplateColumns: "repeat(4,1fr)", gap: 10 }}>
        {apis.map(a => (
          <Card key={a.key} color={a.color} bg={a.bg} border={a.border} style={{ padding: "16px 14px" }} onClick={() => setSel(sel === a.key ? null : a.key)}>
            <div style={{ fontSize: 20, marginBottom: 8 }}>{a.icon}</div>
            <div style={{ fontFamily: "sans-serif", fontSize: 13, fontWeight: 700, color: C.ink, marginBottom: 3 }}>{a.name}</div>
            <div style={{ fontFamily: "monospace", fontSize: 10, color: C.inkDim, lineHeight: 1.6, marginBottom: 8 }}>{a.desc}</div>
            {sel === a.key
              ? <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>{a.methods.map(m => <div key={m} style={{ fontFamily: "monospace", fontSize: 9, color: a.color, background: a.bg, border: `1px solid ${a.border}`, borderRadius: 4, padding: "4px 7px", lineHeight: 1.5 }}>{m}</div>)}</div>
              : <Tag color={a.color} bg={a.bg} border={a.border}>click to expand</Tag>
            }
          </Card>
        ))}
      </div>
    </div>
  );
}

function CoreEngine() {
  const [sel, setSel] = useState(null);
  const modules = [
    { key: "mapper",  icon: "🗺️", color: C.blue,   name: "Object Mapper",    desc: "Zero-reflection mapping via compiled expressions. Faster than Dapper on repeated queries.", details: ["Compiled expression cache", "Convention over configuration", "Custom type converters", "Nullable support"] },
    { key: "builder", icon: "🔨", color: C.amber,  name: "Query Builder",    desc: "Generates parameterized SQL from fluent API. Never concatenates strings.", details: ["WHERE / AND / OR chains", "JOIN support", "GROUP BY + HAVING", "Subquery support"] },
    { key: "rel",     icon: "🔗", color: C.green,  name: "Relation Loader",  desc: "Simple 1:1 and 1:N relations. No lazy loading — always explicit.", details: ["Include<T>()", "1:1 and 1:N only", "Async batched loading", "No N+1 problem"] },
    { key: "tx",      icon: "🔐", color: C.purple, name: "Transaction Scope", desc: "Lightweight async transaction management with automatic rollback.", details: ["BeginTransactionAsync()", "SavepointAsync()", "Auto-rollback on exception", "Nested scope support"] },
    { key: "cache",   icon: "⚡", color: C.teal,   name: "Query Cache",      desc: "Optional in-memory cache for read-heavy queries. TTL-based invalidation.", details: ["Per-query TTL", "Tag-based invalidation", "IMemoryCache backed", "opt-in only"] },
    { key: "migrate", icon: "📋", color: C.rose,   name: "Migrations",       desc: "Lightweight migration runner. Plain SQL files — no DSL to learn.", details: ["Plain .sql files", "Version tracking table", "Up / down migrations", "Multi-db aware"] },
  ];
  return (
    <div>
      <SL>Core Engine Modules</SL>
      <Card style={{ padding: "20px", background: C.surface, border: `1px solid ${C.border}` }}>
        <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 18, fontFamily: "sans-serif", fontSize: 14, fontWeight: 800, color: C.ink }}>
          <span style={{ width: 8, height: 8, borderRadius: "50%", background: C.green, display: "inline-block" }} />
          SlimQuery — Core
          <span style={{ marginLeft: "auto" }}><Tag color={C.green} bg={C.greenBg} border={C.greenBorder}>async first</Tag></span>
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(3,1fr)", gap: 10 }}>
          {modules.map(m => (
            <div key={m.key} onClick={() => setSel(sel === m.key ? null : m.key)}
              style={{ background: sel === m.key ? C.surface2 : C.bg, border: `1px solid ${sel === m.key ? C.borderDark : C.border}`, borderLeft: `3px solid ${m.color}`, borderRadius: 8, padding: "12px 14px", cursor: "pointer", transition: "all 0.18s" }}>
              <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 6 }}>
                <span style={{ fontSize: 16 }}>{m.icon}</span>
                <span style={{ fontFamily: "monospace", fontSize: 11, fontWeight: 700, color: m.color }}>{m.name}</span>
              </div>
              <div style={{ fontFamily: "monospace", fontSize: 10, color: C.inkDim, lineHeight: 1.6 }}>{m.desc}</div>
              {sel === m.key && <div style={{ marginTop: 8, display: "flex", flexDirection: "column", gap: 3 }}>{m.details.map(d => <div key={d} style={{ fontFamily: "monospace", fontSize: 10, color: m.color }}>› {d}</div>)}</div>}
            </div>
          ))}
        </div>
      </Card>
    </div>
  );
}

function DbProviders() {
  return (
    <div>
      <SL>Database Providers</SL>
      <div style={{ display: "grid", gridTemplateColumns: "repeat(4,1fr)", gap: 10 }}>
        {[
          { icon: "🐘", name: "PostgreSQL", tag: "prod",       color: C.blue,   bg: C.blueBg,   border: C.blueBorder },
          { icon: "🗃️", name: "SQLite",     tag: "dev",        color: C.green,  bg: C.greenBg,  border: C.greenBorder },
          { icon: "🪟",  name: "SQL Server", tag: "enterprise", color: C.amber,  bg: C.amberBg,  border: C.amberBorder },
          { icon: "🐬", name: "MySQL",       tag: "optional",   color: C.teal,   bg: C.tealBg,   border: C.tealBorder },
        ].map(p => (
          <Card key={p.name} color={p.color} bg={p.bg} border={p.border} style={{ padding: "14px" }}>
            <div style={{ fontSize: 22, marginBottom: 8 }}>{p.icon}</div>
            <div style={{ fontFamily: "sans-serif", fontSize: 13, fontWeight: 700, color: C.ink, marginBottom: 6 }}>{p.name}</div>
            <Tag color={p.color} bg={p.bg} border={p.border}>{p.tag}</Tag>
          </Card>
        ))}
      </div>
    </div>
  );
}

function Comparison() {
  const features = [
    { name: "Raw SQL",             dapper: true,  ef: true,  slim: true  },
    { name: "Query Builder",       dapper: false, ef: true,  slim: true  },
    { name: "Bulk Operations",     dapper: false, ef: false, slim: true  },
    { name: "Migrations",          dapper: false, ef: true,  slim: true  },
    { name: "1:N Relations",       dapper: false, ef: true,  slim: true  },
    { name: "No Change Tracking",  dapper: true,  ef: false, slim: true  },
    { name: "Compiled Mappings",   dapper: false, ef: false, slim: true  },
    { name: "Zero config models",  dapper: true,  ef: false, slim: true  },
    { name: "Query Cache",         dapper: false, ef: false, slim: true  },
    { name: "Lightweight",         dapper: true,  ef: false, slim: true  },
  ];
  const Check = ({ val }) => <span style={{ fontSize: 14, color: val ? C.green : C.inkMuted }}>{val ? "✓" : "·"}</span>;
  return (
    <div>
      <SL>Feature Comparison</SL>
      <Card style={{ padding: 0, overflow: "hidden" }}>
        <table style={{ width: "100%", borderCollapse: "collapse", fontFamily: "monospace", fontSize: 11 }}>
          <thead>
            <tr style={{ background: C.surface2 }}>
              <th style={{ textAlign: "left", padding: "12px 16px", color: C.inkDim, fontWeight: 600, borderBottom: `1px solid ${C.border}` }}>Feature</th>
              <th style={{ textAlign: "center", padding: "12px 16px", color: C.amber, fontWeight: 600, borderBottom: `1px solid ${C.border}` }}>Dapper</th>
              <th style={{ textAlign: "center", padding: "12px 16px", color: C.blue, fontWeight: 600, borderBottom: `1px solid ${C.border}` }}>EF Core</th>
              <th style={{ textAlign: "center", padding: "12px 16px", color: C.green, fontWeight: 700, borderBottom: `1px solid ${C.border}`, background: C.greenBg }}>SlimQuery ✦</th>
            </tr>
          </thead>
          <tbody>
            {features.map((f, i) => (
              <tr key={f.name} style={{ background: i % 2 === 0 ? C.surface : C.bg }}>
                <td style={{ padding: "10px 16px", color: C.ink, borderBottom: `1px solid ${C.border}` }}>{f.name}</td>
                <td style={{ textAlign: "center", padding: "10px 16px", borderBottom: `1px solid ${C.border}` }}><Check val={f.dapper} /></td>
                <td style={{ textAlign: "center", padding: "10px 16px", borderBottom: `1px solid ${C.border}` }}><Check val={f.ef} /></td>
                <td style={{ textAlign: "center", padding: "10px 16px", borderBottom: `1px solid ${C.border}`, background: i % 2 === 0 ? C.greenBg + "80" : C.greenBg + "40" }}><Check val={f.slim} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>
    </div>
  );
}

export default function SlimQueryDiagram() {
  return (
    <div style={{ background: C.bg, minHeight: "100vh", color: C.ink, padding: "48px 32px", fontFamily: "sans-serif" }}>
      <style>{`* { box-sizing: border-box; }`}</style>
      <div style={{ position: "fixed", inset: 0, pointerEvents: "none", backgroundImage: `radial-gradient(circle, ${C.borderDark} 1px, transparent 1px)`, backgroundSize: "28px 28px", opacity: 0.5 }} />
      <div style={{ maxWidth: 1040, margin: "0 auto", position: "relative", zIndex: 1 }}>

        {/* Header */}
        <div style={{ textAlign: "center", marginBottom: 56 }}>
          <div style={{ display: "inline-block", fontFamily: "monospace", fontSize: 10, letterSpacing: "0.3em", textTransform: "uppercase", color: C.green, border: `1px solid ${C.greenBorder}`, background: C.greenBg, padding: "4px 14px", borderRadius: 4, marginBottom: 16 }}>
            System Design
          </div>
          <div style={{ fontSize: 42, fontWeight: 900, letterSpacing: -2, lineHeight: 1, color: C.ink, marginBottom: 8 }}>SlimQuery</div>
          <div style={{ fontFamily: "monospace", fontSize: 13, color: C.inkDim, marginBottom: 16 }}>
            A micro ORM for .NET — the best of Dapper + EF Core, without the tradeoffs
          </div>
          <div style={{ display: "flex", justifyContent: "center", gap: 8, flexWrap: "wrap" }}>
            {[["Dapper speed", C.amber, C.amberBg, C.amberBorder], ["EF query builder", C.blue, C.blueBg, C.blueBorder], ["Native bulk ops", C.purple, C.purpleBg, C.purpleBorder], ["No change tracking", C.rose, C.roseBg, C.roseBorder], [".NET 10", C.teal, C.tealBg, C.tealBorder]].map(([l, c, b, bo]) => (
              <Tag key={l} color={c} bg={b} border={bo}>{l}</Tag>
            ))}
          </div>
        </div>

        {/* Layers */}
        <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
          <Philosophy />
          <Arrow label="developer calls" />
          <ApiSurface />
          <Arrow label="translates to" />
          <CoreEngine />
          <Arrow label="executes on" />
          <DbProviders />
          <div style={{ height: 1, background: C.border, margin: "8px 0" }} />
          <Comparison />
        </div>

        {/* Footer */}
        <div style={{ marginTop: 48, textAlign: "center", fontFamily: "monospace", fontSize: 11, color: C.inkMuted, display: "flex", alignItems: "center", justifyContent: "center", gap: 16 }}>
          <span>SlimQuery — Micro ORM for .NET</span>
          <span style={{ color: C.border }}>·</span>
          <Tag color={C.green} bg={C.greenBg} border={C.greenBorder}>concept stage</Tag>
        </div>
      </div>
    </div>
  );
}
