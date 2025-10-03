// Auto-added helper to build absolute image URL for attachments and append JWT via ?token=
export function buildImageUrl(path) {
  const base = (import.meta && import.meta.env && (import.meta.env.VITE_API_URL || import.meta.env.VITE_API_BASE)) || ""
  const cleanBase = String(base || "").replace(/\/$/, "")
  const rel = String(path || "").startsWith("/") ? path : "/" + String(path || "")
  const token = (typeof localStorage!=="undefined" && (localStorage.getItem("token") || localStorage.getItem("auth_token"))) || ""
  const url = cleanBase + rel
  return token ? url + (url.includes("?") ? "&" : "?") + "token=" + encodeURIComponent(token) : url
}
