const endpoint = "/graphql";

export function getToken() {
  return localStorage.getItem("jwt") || "";
}

export function setToken(t) {
  localStorage.setItem("jwt", t);
}

export function clearToken() {
  localStorage.removeItem("jwt");
}

export async function gql(query, variables = {}) {
  const res = await fetch("/graphql", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    credentials: "include", // ✅ IMPORTANT: sends Identity cookie
    body: JSON.stringify({ query, variables })
  });

  const json = await res.json();
  if (json.errors) throw new Error(json.errors[0].message);
  return json;
}