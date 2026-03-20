// Tab ID + collision detection
//
// The MMRIA case editor uses a per-tab id to enforce "one editing tab" per user.
// Some browsers duplicate sessionStorage when a tab is duplicated/cloned, which
// can cause two tabs to share the same id and bypass the lock. This module
// detects live collisions and forces the newer tab to regenerate a fresh id.

(function () {

const mmria_tab_id_storage_key = 'mmria_tab_id';

function createGUID() {
  try {
    if (window.crypto && window.crypto.getRandomValues) {
      const buf = new Uint32Array(8);
      window.crypto.getRandomValues(buf);
      const s4 = function (num) {
        const ret = num.toString(16);
        return '00000000'.substring(0, 8 - ret.length) + ret;
      };
      return (
        s4(buf[0]) + s4(buf[1]) + '-' +
        s4(buf[2]).substring(0, 4) + '-' +
        s4(buf[3]).substring(0, 4) + '-' +
        s4(buf[4]).substring(0, 4) + '-' +
        s4(buf[5]) + s4(buf[6]) + s4(buf[7])
      );
    }
  } catch (ex) {
    // fall through to Math.random-based fallback
  }

  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
    const r = Math.random() * 16 | 0;
    const v = c === 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
}

// Some browsers duplicate sessionStorage when a tab is duplicated/cloned.
// If that happens, both tabs would share the same mmria_tab_id and could bypass
// per-tab edit lock enforcement. Detect collisions and force the newer tab to
// regenerate a fresh tab id.
const mmria_tab_page_guid = createGUID();
const mmria_tab_page_created_at = Date.now();
const mmria_tab_id_channel_name = 'mmria_tab_id_collision_channel';
const mmria_tab_id_ls_hello_key = 'mmria_tab_id_collision_hello';
const mmria_tab_id_owner_prefix = 'mmria_tab_id_owner:';
const mmria_tab_id_ls_probe_key = 'mmria_tab_id_collision_probe';

let mmria_tab_id_channel = null;
let mmria_probe_waiters = new Map();

function mmria_safe_json_parse(p_value) {
  try {
    return JSON.parse(p_value);
  } catch (ex) {
    return null;
  }
}

function mmria_get_tab_id_no_init() {
  try {
    return window.sessionStorage.getItem(mmria_tab_id_storage_key);
  } catch (ex) {
    return null;
  }
}

function mmria_set_tab_id_no_init(p_tab_id) {
  try {
    window.sessionStorage.setItem(mmria_tab_id_storage_key, p_tab_id);
  } catch (ex) {
    // ignore
  }
}

function mmria_tab_id_announce(p_tab_id) {
  const payload = {
    kind: 'hello',
    tab_id: p_tab_id,
    page_guid: mmria_tab_page_guid,
    created_at: mmria_tab_page_created_at
  };

  try {
    if (mmria_tab_id_channel) {
      mmria_tab_id_channel.postMessage(payload);
      return;
    }
  } catch (ex) {
    // fall through to localStorage-based announce
  }

  try {
    // localStorage emits a storage event in other tabs
    window.localStorage.setItem(mmria_tab_id_ls_hello_key, JSON.stringify({ ...payload, nonce: createGUID() }));
  } catch (ex) {
    // ignore
  }
}

function mmria_broadcast_message(p_payload) {
  try {
    if (mmria_tab_id_channel) {
      mmria_tab_id_channel.postMessage(p_payload);
      return true;
    }
  } catch (ex) {
    // ignore
  }

  try {
    window.localStorage.setItem(mmria_tab_id_ls_probe_key, JSON.stringify({ ...p_payload, nonce: createGUID() }));
    return true;
  } catch (ex) {
    return false;
  }
}

function mmria_get_navigation_type() {
  try {
    if (window.performance && window.performance.getEntriesByType) {
      const navEntries = window.performance.getEntriesByType('navigation');
      if (navEntries && navEntries.length > 0 && navEntries[0] && navEntries[0].type) {
        return navEntries[0].type;
      }
    }
  } catch (ex) {
    // ignore
  }

  // Legacy fallback
  try {
    if (window.performance && window.performance.navigation) {
      const t = window.performance.navigation.type;
      // 1 is reload in old Navigation API
      if (t === 1) return 'reload';
    }
  } catch (ex) {
    // ignore
  }

  return null;
}

function mmria_claim_tab_id_ownership_or_regenerate(p_tab_id) {
  // Claim ownership for the current page instance.
  // Do not regenerate based only on a stale localStorage owner record because
  // same-tab navigations (logout/login, session restore, overnight resume) can
  // leave ownership behind even when no live conflicting tab exists.
  try {
    if (!p_tab_id) return p_tab_id;

    window.localStorage.setItem(
      mmria_tab_id_owner_prefix + p_tab_id,
      JSON.stringify({ page_guid: mmria_tab_page_guid, created_at: mmria_tab_page_created_at, ts: Date.now() })
    );
  } catch (ex) {
    // localStorage can throw; ignore and rely on async channel detection.
  }

  return p_tab_id;
}

function mmria_release_tab_id_ownership() {
  try {
    const tab_id = mmria_get_tab_id_no_init();
    if (!tab_id) return;

    const key = mmria_tab_id_owner_prefix + tab_id;
    const existing = mmria_safe_json_parse(window.localStorage.getItem(key));

    if (existing && existing.page_guid && existing.page_guid !== mmria_tab_page_guid) {
      return;
    }

    window.localStorage.removeItem(key);
  } catch (ex) {
    // best-effort cleanup only
  }
}

function mmria_should_regenerate_tab_id(p_incoming) {
  // Older tab keeps the id; newer tab regenerates.
  if (!p_incoming) return false;
  if (p_incoming.created_at == null) return false;

  if (mmria_tab_page_created_at > p_incoming.created_at) return true;
  if (mmria_tab_page_created_at < p_incoming.created_at) return false;

  // Tie-breaker if created_at matches (extremely unlikely)
  return (mmria_tab_page_guid > (p_incoming.page_guid || ''));
}

function mmria_handle_probe_message(p_message) {
  if (!p_message) return;

  const current_tab_id = mmria_get_tab_id_no_init();
  if (!current_tab_id) return;

  if (p_message.kind === 'probe')
  {
    if (
      p_message.tab_id &&
      p_message.tab_id === current_tab_id &&
      p_message.page_guid &&
      p_message.page_guid !== mmria_tab_page_guid
    )
    {
      mmria_broadcast_message({
        kind: 'probe-ack',
        probe_id: p_message.probe_id,
        tab_id: current_tab_id,
        page_guid: mmria_tab_page_guid,
        created_at: mmria_tab_page_created_at
      });
    }
    return;
  }

  if (p_message.kind === 'probe-ack')
  {
    const waiter = mmria_probe_waiters.get(p_message.probe_id);
    if (waiter) {
      waiter(true);
      mmria_probe_waiters.delete(p_message.probe_id);
    }
    return;
  }
}

function mmria_handle_incoming_message(p_message) {
  if (!p_message) return;

  if (p_message.kind === 'probe' || p_message.kind === 'probe-ack') {
    mmria_handle_probe_message(p_message);
    return;
  }

  // Back-compat for any message without kind: treat as hello
  if (!p_message.kind) {
    p_message.kind = 'hello';
  }

  if (p_message.kind === 'hello') {
    mmria_handle_tab_id_collision_message(p_message);
  }
}

async function mmria_get_unique_tab_id() {
  // Active handshake: if another live tab is currently using our tab id,
  // regenerate immediately in this tab.
  try {
    mmria_init_tab_id_collision_detection();

    let tab_id = mmria_get_tab_id_no_init();
    if (!tab_id) {
      tab_id = createGUID();
      mmria_set_tab_id_no_init(tab_id);
    }

    const probe_id = createGUID();
    const collisionDetected = await new Promise((resolve) => {
      mmria_probe_waiters.set(probe_id, resolve);

      const payload = {
        kind: 'probe',
        probe_id,
        tab_id,
        page_guid: mmria_tab_page_guid,
        created_at: mmria_tab_page_created_at
      };

      // Send a few probes to survive scheduling/network hiccups.
      mmria_broadcast_message(payload);
      window.setTimeout(function () { mmria_broadcast_message(payload); }, 50);
      window.setTimeout(function () { mmria_broadcast_message(payload); }, 125);

      // Give the other tab time to respond.
      window.setTimeout(function () {
        if (mmria_probe_waiters.has(probe_id)) {
          mmria_probe_waiters.delete(probe_id);
          resolve(false);
        }
      }, 350);
    });

    if (collisionDetected)
    {
      const new_tab_id = createGUID();
      mmria_set_tab_id_no_init(new_tab_id);
      tab_id = new_tab_id;
    }

    tab_id = mmria_claim_tab_id_ownership_or_regenerate(tab_id);
    mmria_tab_id_announce(tab_id);
    return tab_id;
  } catch (ex) {
    // best-effort
    return get_mmria_tab_id();
  }
}

function mmria_handle_tab_id_collision_message(p_message) {
  const current_tab_id = mmria_get_tab_id_no_init();
  if (!current_tab_id) return;

  if (
    p_message &&
    p_message.tab_id &&
    p_message.tab_id === current_tab_id &&
    p_message.page_guid &&
    p_message.page_guid !== mmria_tab_page_guid
  )
  {
    if (mmria_should_regenerate_tab_id(p_message))
    {
      const new_tab_id = createGUID();
      mmria_set_tab_id_no_init(new_tab_id);
      mmria_tab_id_announce(new_tab_id);
    }
  }
}

function mmria_init_tab_id_collision_detection() {
  if (mmria_tab_id_channel || typeof window === 'undefined') return;

  try {
    if (typeof BroadcastChannel !== 'undefined')
    {
      mmria_tab_id_channel = new BroadcastChannel(mmria_tab_id_channel_name);
      mmria_tab_id_channel.onmessage = function (ev) {
        mmria_handle_incoming_message(ev && ev.data ? ev.data : null);
      };
    }
  } catch (ex) {
    mmria_tab_id_channel = null;
  }

  if (!mmria_tab_id_channel)
  {
    try {
      window.addEventListener('storage', function (ev) {
        if (!ev) return;
        if (ev.key !== mmria_tab_id_ls_hello_key && ev.key !== mmria_tab_id_ls_probe_key) return;
        mmria_handle_incoming_message(mmria_safe_json_parse(ev.newValue));
      });
    } catch (ex) {
      // ignore
    }
  }

  // Announce on a short delay so storage/channel listeners are attached.
  try {
    window.setTimeout(function () {
      const tab_id = mmria_get_tab_id_no_init();
      if (tab_id) mmria_tab_id_announce(tab_id);
    }, 0);
  } catch (ex) {
    // ignore
  }

  try {
    window.addEventListener('pagehide', mmria_release_tab_id_ownership);
    window.addEventListener('beforeunload', mmria_release_tab_id_ownership);
  } catch (ex) {
    // ignore
  }
}

function get_mmria_tab_id() {
  try {
    mmria_init_tab_id_collision_detection();

    let tab_id = window.sessionStorage.getItem(mmria_tab_id_storage_key);
    if (!tab_id) {
      tab_id = createGUID();
      window.sessionStorage.setItem(mmria_tab_id_storage_key, tab_id);
    }

    tab_id = mmria_claim_tab_id_ownership_or_regenerate(tab_id);

    // Always announce our current tab id so other tabs can detect collisions.
    // Also re-check collision messages by announcing when first accessed.
    mmria_tab_id_announce(tab_id);

    return tab_id;
  } catch (ex) {
    // sessionStorage can throw in some locked-down browser contexts; still return a best-effort id.
    return createGUID();
  }
}

// Initialize once at script load (best-effort) so collision detection works
// even before the first explicit call path that needs a tab id.
try {
  mmria_init_tab_id_collision_detection();
  window.get_mmria_tab_id = get_mmria_tab_id;
  window.mmria_get_unique_tab_id = mmria_get_unique_tab_id;
  // Kick off an early uniqueness check so duplicated tabs diverge quickly.
  mmria_get_unique_tab_id();
} catch (ex) {
  // ignore
}

// Always export (even if init try/catch above throws before assignment)
try {
  window.get_mmria_tab_id = get_mmria_tab_id;
  window.mmria_get_unique_tab_id = mmria_get_unique_tab_id;
} catch (ex) {
  // ignore
}

})();
