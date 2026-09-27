/**
 * FolderMount Website — Interactive Logic & App Simulator
 * High Performance, Dependency-Free Vanilla JavaScript
 */

document.addEventListener('DOMContentLoaded', () => {
  ThemeManager.init();
  initAppSimulator();
  initPathShortener();
  initCopyButtons();
  initMobileNav();
});

/* ==========================================================================
   0. Theme Switcher System (Follows Windows System Default / Dark / Light)
   ========================================================================== */
const ThemeManager = {
  STORAGE_KEY: 'foldermount_theme',

  init() {
    const currentMode = this.getThemeMode();
    this.applyTheme(currentMode, false);

    // Dynamic OS Theme Preference Listener
    if (window.matchMedia) {
      const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
      const handleSystemThemeChange = () => {
        if (this.getThemeMode() === 'system') {
          this.applyTheme('system', false);
        }
      };

      if (mediaQuery.addEventListener) {
        mediaQuery.addEventListener('change', handleSystemThemeChange);
      } else if (mediaQuery.addListener) {
        mediaQuery.addListener(handleSystemThemeChange);
      }
    }

    this.bindEvents();
  },

  getThemeMode() {
    try {
      const saved = localStorage.getItem(this.STORAGE_KEY);
      if (saved === 'dark' || saved === 'light' || saved === 'system') {
        return saved;
      }
    } catch (e) {}
    return 'system';
  },

  getResolvedTheme(mode) {
    if (mode === 'dark') return 'dark';
    if (mode === 'light') return 'light';
    // 'system': follow OS preference
    return (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) ? 'dark' : 'light';
  },

  setTheme(mode) {
    if (mode !== 'system' && mode !== 'dark' && mode !== 'light') {
      mode = 'system';
    }
    try {
      localStorage.setItem(this.STORAGE_KEY, mode);
    } catch (e) {}
    this.applyTheme(mode, true);
  },

  cycleTheme() {
    const current = this.getThemeMode();
    const next = current === 'system' ? 'dark' : (current === 'dark' ? 'light' : 'system');
    this.setTheme(next);
  },

  applyTheme(mode, notify) {
    const resolved = this.getResolvedTheme(mode);
    document.documentElement.setAttribute('data-theme', resolved);
    document.documentElement.setAttribute('data-theme-mode', mode);

    const metaThemeColor = document.querySelector('meta[name="theme-color"]');
    if (metaThemeColor) {
      metaThemeColor.setAttribute('content', resolved === 'dark' ? '#1e1e28' : '#f8fafc');
    }

    this.updateUI(mode, resolved);
  },

  updateUI(mode, resolved) {
    const icons = {
      system: '💻',
      dark: '🌙',
      light: '☀️'
    };
    const labels = {
      system: 'System',
      dark: 'Dark',
      light: 'Light'
    };

    // Navbar switcher button
    const indicatorIcon = document.getElementById('theme-indicator-icon');
    const indicatorLabel = document.getElementById('theme-indicator-label');
    if (indicatorIcon) indicatorIcon.textContent = icons[mode] || '💻';
    if (indicatorLabel) indicatorLabel.textContent = labels[mode] || 'System';

    // Dropdown items
    document.querySelectorAll('.theme-menu-item').forEach(item => {
      const itemVal = item.getAttribute('data-theme-val');
      const isActive = itemVal === mode;
      item.classList.toggle('active', isActive);
      item.setAttribute('aria-selected', isActive ? 'true' : 'false');
    });

    // Mobile buttons
    document.querySelectorAll('.mobile-theme-btn').forEach(btn => {
      const btnVal = btn.getAttribute('data-theme-val');
      btn.classList.toggle('active', btnVal === mode);
    });

    // Desktop Simulator Toolbar button
    const simIcon = document.getElementById('sim-theme-icon');
    const simBtn = document.getElementById('sim-theme-toggle');
    if (simIcon) simIcon.textContent = icons[mode] || '💻';
    if (simBtn) {
      const nextMode = mode === 'system' ? 'Dark' : mode === 'dark' ? 'Light' : 'System';
      simBtn.setAttribute('title', `Theme: ${labels[mode]} (Click to switch to ${nextMode})`);
    }
  },

  bindEvents() {
    const toggleBtn = document.getElementById('theme-toggle-btn');
    const dropdown = document.getElementById('theme-dropdown-menu');

    if (toggleBtn && dropdown) {
      toggleBtn.addEventListener('click', (e) => {
        e.stopPropagation();
        const isOpen = dropdown.classList.toggle('show');
        toggleBtn.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
      });

      document.addEventListener('click', (e) => {
        if (!e.target.closest('#theme-switcher')) {
          dropdown.classList.remove('show');
          toggleBtn.setAttribute('aria-expanded', 'false');
        }
      });

      document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && dropdown.classList.contains('show')) {
          dropdown.classList.remove('show');
          toggleBtn.setAttribute('aria-expanded', 'false');
          toggleBtn.focus();
        }
      });
    }

    // Dropdown items
    document.querySelectorAll('.theme-menu-item').forEach(item => {
      item.addEventListener('click', (e) => {
        e.stopPropagation();
        const val = item.getAttribute('data-theme-val');
        if (val) {
          this.setTheme(val);
          if (dropdown) dropdown.classList.remove('show');
          if (toggleBtn) toggleBtn.setAttribute('aria-expanded', 'false');
        }
      });
    });

    // Mobile buttons
    document.querySelectorAll('.mobile-theme-btn').forEach(btn => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const val = btn.getAttribute('data-theme-val');
        if (val) {
          this.setTheme(val);
        }
      });
    });

    // Desktop Simulator Toolbar theme button
    const simBtn = document.getElementById('sim-theme-toggle');
    if (simBtn) {
      simBtn.addEventListener('click', (e) => {
        e.stopPropagation();
        this.cycleTheme();
      });
    }
  }
};


/* ==========================================================================
   1. Interactive FolderMount Desktop App Simulator
   ========================================================================== */
function initAppSimulator() {
  const defaultMappings = [
    { id: 1, letter: 'P', path: 'D:\\Projects\\WebApps\\CorePlatform', label: 'Work Projects', active: true },
    { id: 2, letter: 'M', path: 'E:\\Media\\VideoProduction\\4K_B-Roll', label: 'Media Library', active: true },
    { id: 3, letter: 'X', path: 'C:\\AI_Research\\DeepLearning\\Datasets', label: 'ML Datasets', active: false },
    { id: 4, letter: 'Z', path: 'D:\\ClientVault\\AlphaClient\\2026', label: 'Client Share', active: true }
  ];

  let mappings = [...defaultMappings];

  // DOM Elements
  const gridBody = document.getElementById('sim-grid-body');
  const activeCountEl = document.getElementById('sim-active-count');
  const totalCountEl = document.getElementById('sim-total-count');
  const bottomTotalEl = document.getElementById('sim-bottom-total');
  const statusMsgEl = document.getElementById('sim-status-msg');
  const cliCmdEl = document.getElementById('sim-cli-cmd');
  const mountAllBtn = document.getElementById('sim-mount-all');
  const unmountAllBtn = document.getElementById('sim-unmount-all');
  const refreshBtn = document.getElementById('sim-refresh');
  const addBtn = document.getElementById('sim-add-btn');

  // Modal Elements
  const modalOverlay = document.getElementById('sim-modal-overlay');
  const modalCloseBtn = document.getElementById('sim-modal-close');
  const modalCancelBtn = document.getElementById('sim-modal-cancel');
  const modalForm = document.getElementById('sim-modal-form');
  const inputLetter = document.getElementById('sim-new-letter');
  const inputPath = document.getElementById('sim-new-path');
  const inputLabel = document.getElementById('sim-new-label');

  if (!gridBody) return;

  function renderGrid() {
    gridBody.innerHTML = '';

    mappings.forEach(item => {
      const tr = document.createElement('tr');
      tr.id = `sim-row-${item.id}`;

      tr.innerHTML = `
        <td>
          <div class="sim-status-cell">
            <span class="sim-status-dot ${item.active ? 'status-active' : 'status-inactive'}"></span>
            <span style="font-weight: 500; color: ${item.active ? 'var(--accent-success)' : 'var(--text-muted)'};">
              ${item.active ? 'Mounted' : 'Ejected'}
            </span>
          </div>
        </td>
        <td>
          <span class="sim-drive-badge">${item.letter}:</span>
        </td>
        <td class="sim-path-cell">
          <span>${escapeHtml(item.path)}</span>
        </td>
        <td class="sim-label-cell">
          <span>${escapeHtml(item.label)}</span>
        </td>
        <td>
          <div class="sim-action-cell">
            <button class="sim-act-btn ${item.active ? 'btn-eject' : 'btn-mount'}" data-action="toggle" data-id="${item.id}" title="${item.active ? 'Eject virtual drive' : 'Mount virtual drive'}">
              ${item.active ? '⏏ Eject' : '⚡ Mount'}
            </button>
            <span style="color: #3f3f5a;">|</span>
            <button class="sim-act-btn" data-action="open" data-id="${item.id}" ${item.active ? '' : 'disabled style="opacity: 0.4; cursor: not-allowed;"'} title="Open virtual drive in Explorer">
              📂 Open
            </button>
            <button class="sim-act-btn" data-action="delete" data-id="${item.id}" style="color: var(--accent-danger);" title="Remove mapping">
              🗑
            </button>
          </div>
        </td>
      `;

      gridBody.appendChild(tr);
    });

    updateCounters();
  }

  function updateCounters() {
    const activeCount = mappings.filter(m => m.active).length;
    const totalCount = mappings.length;

    if (activeCountEl) activeCountEl.textContent = `${activeCount} Active`;
    if (totalCountEl) totalCountEl.textContent = `${totalCount} Total`;
    if (bottomTotalEl) bottomTotalEl.textContent = `Mappings: ${totalCount}`;
  }

  function setCommand(cmd, statusMsg) {
    if (cliCmdEl) {
      cliCmdEl.textContent = cmd;
      cliCmdEl.style.animation = 'none';
      // Trigger reflow
      void cliCmdEl.offsetWidth;
      cliCmdEl.style.color = '#38bdf8';
    }
    if (statusMsgEl && statusMsg) {
      statusMsgEl.textContent = statusMsg;
    }
  }

  // Row Action Delegations
  gridBody.addEventListener('click', (e) => {
    const btn = e.target.closest('button[data-action]');
    if (!btn) return;

    const action = btn.getAttribute('data-action');
    const id = parseInt(btn.getAttribute('data-id'), 10);
    const item = mappings.find(m => m.id === id);
    if (!item) return;

    if (action === 'toggle') {
      item.active = !item.active;
      if (item.active) {
        setCommand(`SUBST ${item.letter}: "${item.path}"`, `Virtual drive ${item.letter}: mounted successfully.`);
      } else {
        setCommand(`SUBST ${item.letter}: /D`, `Virtual drive ${item.letter}: ejected.`);
      }
      renderGrid();
    } else if (action === 'open') {
      setCommand(`explorer.exe "${item.letter}:\\"`, `Opening virtual drive ${item.letter}: in Windows File Explorer...`);
    } else if (action === 'delete') {
      if (item.active) {
        setCommand(`SUBST ${item.letter}: /D`, `Ejected and deleted mapping for ${item.letter}:`);
      } else {
        setCommand(`<!-- Removed ${item.letter}: mapping from mappings.xml -->`, `Mapping for ${item.letter}: removed.`);
      }
      mappings = mappings.filter(m => m.id !== id);
      renderGrid();
    }
  });

  // Bulk Actions
  if (mountAllBtn) {
    mountAllBtn.addEventListener('click', () => {
      mappings.forEach(m => m.active = true);
      renderGrid();
      setCommand(`FolderMount /mountall (mounted ${mappings.length} virtual drives)`, `All drives mounted successfully.`);
    });
  }

  if (unmountAllBtn) {
    unmountAllBtn.addEventListener('click', () => {
      mappings.forEach(m => m.active = false);
      renderGrid();
      setCommand(`FolderMount /unmountall (ejected all virtual drives)`, `All virtual drives ejected.`);
    });
  }

  if (refreshBtn) {
    refreshBtn.addEventListener('click', () => {
      renderGrid();
      setCommand(`FolderMount: scanned active Windows SUBST table`, `Drive status refreshed.`);
    });
  }

  // Add Mapping Modal
  function openModal() {
    if (!modalOverlay) return;
    // Populate available drive letters
    if (inputLetter) {
      inputLetter.innerHTML = '';
      const occupied = mappings.map(m => m.letter);
      const letters = ['P', 'M', 'X', 'Z', 'V', 'W', 'K', 'T', 'S', 'R', 'Q', 'O', 'N', 'L', 'J', 'I', 'H', 'G', 'F', 'E', 'D', 'B', 'A'];
      letters.forEach(letter => {
        if (!occupied.includes(letter)) {
          const opt = document.createElement('option');
          opt.value = letter;
          opt.textContent = `${letter}:`;
          inputLetter.appendChild(opt);
        }
      });
    }
    modalOverlay.classList.add('active');
  }

  function closeModal() {
    if (modalOverlay) modalOverlay.classList.remove('active');
  }

  if (addBtn) addBtn.addEventListener('click', openModal);
  if (modalCloseBtn) modalCloseBtn.addEventListener('click', closeModal);
  if (modalCancelBtn) modalCancelBtn.addEventListener('click', closeModal);

  if (modalOverlay) {
    modalOverlay.addEventListener('click', (e) => {
      if (e.target === modalOverlay) closeModal();
    });
  }

  if (modalForm) {
    modalForm.addEventListener('submit', (e) => {
      e.preventDefault();
      const letter = inputLetter ? inputLetter.value : 'V';
      const path = inputPath ? inputPath.value.trim() : 'D:\\Projects\\NewApp';
      const label = inputLabel ? inputLabel.value.trim() : 'My Drive';

      if (!path) return;

      const newId = mappings.length ? Math.max(...mappings.map(m => m.id)) + 1 : 1;
      mappings.push({
        id: newId,
        letter: letter,
        path: path,
        label: label || 'Custom Mapping',
        active: true
      });

      closeModal();
      renderGrid();
      setCommand(`SUBST ${letter}: "${path}"`, `Added and mounted virtual drive ${letter}:`);
      if (inputPath) inputPath.value = '';
      if (inputLabel) inputLabel.value = '';
    });
  }

  // Initial render
  renderGrid();
  setCommand('Ready. SUBST system table initialized.', 'Ready — 3 active virtual drives');
}

/* ==========================================================================
   2. Interactive MAX_PATH / Path Shortener Tool
   ========================================================================== */
function initPathShortener() {
  const pathInput = document.getElementById('path-tester-input');
  const letterSelect = document.getElementById('path-tester-letter');
  const origLenEl = document.getElementById('orig-path-len');
  const shortLenEl = document.getElementById('short-path-len');
  const origDisplayEl = document.getElementById('orig-path-display');
  const shortDisplayEl = document.getElementById('short-path-display');
  const charsSavedEl = document.getElementById('chars-saved');
  const presetButtons = document.querySelectorAll('.preset-btn');

  if (!pathInput || !origDisplayEl || !shortDisplayEl) return;

  function updatePathDemo() {
    const rawPath = pathInput.value.trim() || 'C:\\Users\\AlexDev\\Projects\\EnterpriseApp\\client\\node_modules\\@azure\\identity\\node_modules\\msal-browser\\dist';
    const driveLetter = letterSelect ? letterSelect.value : 'P';

    // Original Path metrics
    const origLen = rawPath.length;
    origDisplayEl.textContent = rawPath;
    if (origLenEl) origLenEl.textContent = `${origLen} chars`;

    // Simulated virtual drive path
    // e.g. mapping the root of the project to P:
    const pathParts = rawPath.split(/[\\\/]/).filter(Boolean);
    let shortPath = `${driveLetter}:\\`;
    if (pathParts.length > 3) {
      shortPath += pathParts.slice(-2).join('\\');
    } else if (pathParts.length > 0) {
      shortPath += pathParts[pathParts.length - 1];
    }

    const shortLen = shortPath.length;
    shortDisplayEl.textContent = shortPath;
    if (shortLenEl) shortLenEl.textContent = `${shortLen} chars`;

    const saved = Math.max(0, origLen - shortLen);
    if (charsSavedEl) charsSavedEl.textContent = `${saved} characters saved (${Math.round((saved / origLen) * 100)}% reduction)`;
  }

  pathInput.addEventListener('input', updatePathDemo);
  if (letterSelect) letterSelect.addEventListener('change', updatePathDemo);

  presetButtons.forEach(btn => {
    btn.addEventListener('click', () => {
      const preset = btn.getAttribute('data-preset');
      if (preset) {
        pathInput.value = preset;
        updatePathDemo();
      }
    });
  });

  updatePathDemo();
}

/* ==========================================================================
   3. Copy-to-Clipboard Utility
   ========================================================================== */
function initCopyButtons() {
  document.querySelectorAll('[data-copy]').forEach(btn => {
    btn.addEventListener('click', async () => {
      const text = btn.getAttribute('data-copy');
      if (!text) return;

      try {
        await navigator.clipboard.writeText(text);
        const origText = btn.innerHTML;
        btn.innerHTML = '✓ Copied!';
        btn.style.color = 'var(--accent-success)';
        btn.style.borderColor = 'var(--accent-success)';

        setTimeout(() => {
          btn.innerHTML = origText;
          btn.style.color = '';
          btn.style.borderColor = '';
        }, 2000);
      } catch (err) {
        console.error('Failed to copy', err);
      }
    });
  });
}

/* ==========================================================================
   4. Mobile Navigation
   ========================================================================== */
function initMobileNav() {
  const toggleBtn = document.querySelector('.mobile-toggle');
  const navMenu = document.querySelector('.nav-menu');

  if (!toggleBtn || !navMenu) return;

  toggleBtn.addEventListener('click', () => {
    const isOpen = navMenu.classList.toggle('open');
    toggleBtn.setAttribute('aria-expanded', isOpen);
    toggleBtn.textContent = isOpen ? '✕' : '☰';
  });

  navMenu.querySelectorAll('.nav-link').forEach(link => {
    link.addEventListener('click', () => {
      navMenu.classList.remove('open');
      toggleBtn.textContent = '☰';
      toggleBtn.setAttribute('aria-expanded', 'false');
    });
  });
}

/* ==========================================================================
   5. Dynamic Canonical & Subpage URL handling
   ========================================================================== */
function initDynamicCanonical() {
  try {
    const canonical = document.querySelector('link[rel="canonical"]');
    if (canonical && window.location.origin && !window.location.origin.includes('file://')) {
      // Normalize pathname to ensure subpage compatibility
      const url = new URL(window.location.href);
      url.search = '';
      url.hash = '';
      canonical.setAttribute('href', url.toString());
    }
  } catch (e) {
    // Ignore in local preview
  }
}

// Utility
function escapeHtml(str) {
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}
