function toast(msg) {
  const el = document.getElementById("toast");
  if (!el) { alert(msg); return; }
  el.textContent = msg;
  el.style.opacity = "1";
  setTimeout(() => el.style.opacity = "0", 2500);
}

function money(n) {
  if (n === null || n === undefined) return "–";
  return new Intl.NumberFormat("en-US", { style:"currency", currency:"PLN" }).format(n);
}

function ui_showToken() {
  const t = localStorage.getItem("token");
  if (!t) return toast("No token saved");
  alert(t);
}

function ui_logout() {
  localStorage.removeItem("token");
  toast("Logged out");
  window.location.href = "/";
}

// Expose globally (your cshtml calls these)
window.toast = toast;
window.money = money;
window.ui_showToken = ui_showToken;
window.ui_logout = ui_logout;

window.ui_getUserLabel = function () {
  try {
    const token = localStorage.getItem("token");
    if (!token) return "Guest";
    const payload = JSON.parse(atob(token.split(".")[1]));
    return payload.email || payload.unique_name || payload.sub || "User";
  } catch {
    return "User";
  }
};

async function hh_refresh() {
  const query = `
    query {
      households {
        id
        name
        isMember
      }
    }
  `;

  const res = await api_gql(query);
  document.getElementById("hhRaw").textContent = JSON.stringify(res, null, 2);

  const tbody = document.querySelector("#hhTable tbody");
  tbody.innerHTML = "";

  const list = res?.data?.households ?? [];
  if (list.length === 0) {
    tbody.innerHTML = `<tr><td colspan="3" class="muted">No households found.</td></tr>`;
    return;
  }

  for (const h of list) {
    const canOpen = !!h.isMember;
    const action = canOpen
      ? `<a class="link" href="/Households/Details?id=${h.id}">Open</a>`
      : `<span class="muted">Join first</span>`;

    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${h.id}</td>
      <td>${escapeHtml(h.name)}</td>
      <td>${action}</td>
    `;
    tbody.appendChild(tr);
  }
}

async function hh_create() {
  const name = document.getElementById("hhName").value.trim();
  if (!name) return toast("Enter a household name");

  const mutation = `
    mutation($name:String!) {
      createHousehold(name:$name) {
        id
        name
        isMember
      }
    }
  `;

  const res = await api_gql(mutation, { name });
  document.getElementById("hhRaw").textContent = JSON.stringify(res, null, 2);

  if (res?.errors?.length) return toast(res.errors[0].message);
  toast("Household created ✅");
  document.getElementById("hhName").value = "";
  await hh_refresh();

  // update Dashboard KPIs if you want later
}

async function hh_join() {
  const raw = document.getElementById("hhJoinId").value.trim();
  const id = parseInt(raw, 10);
  if (!id) return toast("Enter a valid household ID");

  const mutation = `
    mutation($id:Int!) {
      joinHousehold(householdId:$id)
    }
  `;

  const res = await api_gql(mutation, { id });
  document.getElementById("hhRaw").textContent = JSON.stringify(res, null, 2);

  if (res?.errors?.length) return toast(res.errors[0].message);
  toast("Joined ✅");
  await hh_refresh();
}

// helpers
function escapeHtml(str) {
  return (str ?? "")
    .replaceAll("&","&amp;")
    .replaceAll("<","&lt;")
    .replaceAll(">","&gt;")
    .replaceAll('"',"&quot;")
    .replaceAll("'","&#039;");
}

// expose to window (Razor onclick)
window.hh_refresh = hh_refresh;
window.hh_create = hh_create;
window.hh_join = hh_join;

function hh_getIdFromQuery() {
  const u = new URL(window.location.href);
  return parseInt(u.searchParams.get("id") || "0", 10);
}

async function hh_details_refresh() {
  const householdId = hh_getIdFromQuery();
  if (!householdId) {
    document.getElementById("hhDetailStatus").textContent = "Missing household id in URL";
    return;
  }

  const query = `
    query($id:Int!) {
      expenses(householdId:$id) {
        id
        amount
        date
        description
        paidByEmail
      }
      balances(householdId:$id) {
        userId
        email
        totalPaid
        totalOwed
        balance
      }
    }
  `;

  const res = await api_gql(query, { id: householdId });
  document.getElementById("hhDetailRaw").textContent = JSON.stringify(res, null, 2);

  if (res?.errors?.length) {
    document.getElementById("hhDetailStatus").textContent = res.errors[0].message;
    return;
  }

  document.getElementById("hhDetailStatus").textContent = "Loaded ✅";

  // expenses table
  const expBody = document.querySelector("#expTable tbody");
  expBody.innerHTML = "";
  const exps = res?.data?.expenses ?? [];
  if (!exps.length) {
    expBody.innerHTML = `<tr><td colspan="4" class="muted">No expenses yet.</td></tr>`;
  } else {
    for (const e of exps) {
      const d = (e.date || "").substring(0,10);
      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td>${d}</td>
        <td>${escapeHtml(e.description)}</td>
        <td>${money(e.amount)}</td>
        <td>${escapeHtml(e.paidByEmail)}</td>
      `;
      expBody.appendChild(tr);
    }
  }

  // balances table
  const balBody = document.querySelector("#balTable tbody");
  balBody.innerHTML = "";
  const bals = res?.data?.balances ?? [];
  if (!bals.length) {
    balBody.innerHTML = `<tr><td colspan="4" class="muted">No balance data.</td></tr>`;
  } else {
    for (const b of bals) {
      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td>${escapeHtml(b.email)}</td>
        <td>${money(b.totalPaid)}</td>
        <td>${money(b.totalOwed)}</td>
        <td>${money(b.balance)}</td>
      `;
      balBody.appendChild(tr);
    }
  }
}

async function hh_addExpense() {
  const householdId = hh_getIdFromQuery();
  if (!householdId) return toast("Missing household id");

  const desc = document.getElementById("exDesc").value.trim();
  const amt = parseFloat(document.getElementById("exAmount").value.trim());
  const date = document.getElementById("exDate").value;
  const splitEqually = document.getElementById("exSplit").checked;

  if (!desc) return toast("Enter description");
  if (!amt || amt <= 0) return toast("Enter a valid amount");
  if (!date) return toast("Pick a date");

  const mutation = `
    mutation($input:CreateExpenseInput!) {
      createExpense(input:$input) {
        id
      }
    }
  `;

  const input = {
    householdId,
    amount: amt,
    description: desc,
    date: date,
    splitEqually,
    shares: null
  };

  const res = await api_gql(mutation, { input });
  document.getElementById("hhDetailRaw").textContent = JSON.stringify(res, null, 2);

  if (res?.errors?.length) return toast(res.errors[0].message);

  toast("Expense added ✅");
  document.getElementById("exDesc").value = "";
  document.getElementById("exAmount").value = "";
  await hh_details_refresh();
}

window.hh_details_refresh = hh_details_refresh;
window.hh_addExpense = hh_addExpense;