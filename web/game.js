(() => {
  "use strict";

  const canvas = document.querySelector("#game");
  const ctx = canvas.getContext("2d", { alpha: true, desynchronized: true });
  const ui = {
    app: document.querySelector("#app"),
    world: document.querySelector("#world"),
    hud: document.querySelector("#hud"),
    score: document.querySelector("#score"),
    crystals: document.querySelector("#crystals"),
    menuCurrency: document.querySelector("#menu-currency"),
    best: document.querySelector("#best-score"),
    menuBird: document.querySelector("#menu-bird"),
    equipped: document.querySelector("#equipped-copy"),
    menu: document.querySelector("#menu"),
    customize: document.querySelector("#customize"),
    shopCurrency: document.querySelector("#shop-currency"),
    collection: document.querySelector("#collection-progress"),
    shopGrid: document.querySelector("#shop-grid"),
    shopTabs: document.querySelector("#shop-tabs"),
    gameOver: document.querySelector("#game-over"),
    resultScore: document.querySelector("#result-score"),
    resultBest: document.querySelector("#result-best"),
    resultCrystals: document.querySelector("#result-crystals"),
    resultCelebration: document.querySelector("#result-celebration"),
    resultMedal: document.querySelector("#result-medal"),
    resultMilestone: document.querySelector("#result-milestone"),
    resultShare: document.querySelector("#share-result"),
    pause: document.querySelector("#pause"),
    settings: document.querySelector("#settings"),
    sound: document.querySelector("#sound-toggle"),
    motion: document.querySelector("#motion-toggle"),
    contrast: document.querySelector("#contrast-toggle"),
    flightHint: document.querySelector("#flight-hint"),
    daily: document.querySelector("#daily"),
    dailyDate: document.querySelector("#daily-date"),
    dailyList: document.querySelector("#daily-list"),
    resultDaily: document.querySelector("#result-daily"),
    feedback: document.querySelector("#feedback-button"),
    toast: document.querySelector("#toast"),
  };

  const ASSET = "../assets/";
  const skins = [
    { id: "nova", name: "NOVA", price: 0, accent: "#8f64ff", trail: ["#8f64ff", "#45eaff"], art: "images/characters/nova.png", flap: "images/characters/nova-flap.png" },
    { id: "lumen", name: "LUMEN", price: 20, accent: "#45eaff", trail: ["#45eaff", "#8f64ff"], art: "images/characters/lumen.png", flap: "images/characters/lumen-flap.png" },
    { id: "ember", name: "EMBER", price: 55, accent: "#f05bc6", trail: ["#f05bc6", "#ffc34d"], art: "images/characters/ember.png", flap: "images/characters/ember-flap.png" },
    { id: "sol", name: "SOL", price: 110, accent: "#ffc34d", trail: ["#ffc34d", "#45eaff"], art: "images/characters/sol.png", flap: "images/characters/sol-flap.png" },
    { id: "aurora", name: "AURORA", price: 80, accent: "#61f5b3", trail: ["#61f5b3", "#45eaff"], art: "images/characters/lumen.png", flap: "images/characters/lumen-flap.png", tint: "#b8ffe1" },
    { id: "orchid", name: "ORCHID", price: 105, accent: "#b17cff", trail: ["#b17cff", "#f05bc6"], art: "images/characters/nova.png", flap: "images/characters/nova-flap.png", tint: "#e7adff" },
    { id: "coral", name: "CORAL", price: 135, accent: "#f082af", trail: ["#f082af", "#ffc34d"], art: "images/characters/ember.png", flap: "images/characters/ember-flap.png", tint: "#ffb3cf" },
    { id: "glacier", name: "GLACIER", price: 165, accent: "#d7f1ff", trail: ["#d7f1ff", "#45eaff"], art: "images/characters/sol.png", flap: "images/characters/sol-flap.png", tint: "#c4eaff" },
    { id: "aether", name: "PRISM", price: 190, accent: "#45eaff", trail: ["#45eaff", "#edf7ff"], art: "images/characters/generated/prism.png", flap: "images/characters/generated/prism-flap.png", scale: .76 },
    { id: "verdant", name: "VERDANT", price: 215, accent: "#61f5b3", trail: ["#61f5b3", "#45eaff"], art: "images/characters/generated/verdant.png", flap: "images/characters/generated/verdant-flap.png", scale: .74 },
    { id: "ruby", name: "CINDER", price: 240, accent: "#f05bc6", trail: ["#f05bc6", "#ffc34d"], art: "images/characters/generated/cinder.png", flap: "images/characters/generated/cinder-flap.png", scale: .76 },
    { id: "onyx", name: "TIDE", price: 265, accent: "#45eaff", trail: ["#45eaff", "#8f64ff"], art: "images/characters/generated/tide.png", flap: "images/characters/generated/tide-flap.png", scale: .78 },
    { id: "moon", name: "WISP", price: 290, accent: "#edf7ff", trail: ["#edf7ff", "#45eaff"], art: "images/characters/generated/wisp.png", flap: "images/characters/generated/wisp-flap.png", scale: .73 },
    { id: "amethyst", name: "BLOOM", price: 315, accent: "#f05bc6", trail: ["#f05bc6", "#b17cff"], art: "images/characters/generated/bloom.png", flap: "images/characters/generated/bloom-flap.png", scale: .79 },
    { id: "flare", name: "EMBERWING", price: 340, accent: "#ffc34d", trail: ["#ffc34d", "#f05bc6"], art: "images/characters/generated/emberwing.png", flap: "images/characters/generated/emberwing-flap.png", scale: .73 },
    { id: "arctic", name: "STEEL", price: 365, accent: "#edf7ff", trail: ["#edf7ff", "#45eaff"], art: "images/characters/generated/steel.png", flap: "images/characters/generated/steel-flap.png", scale: .78 },
  ];

  const themes = [
    { id: "neon_city", name: "NEON CITY", price: 0, accent: "#45eaff", background: "images/backgrounds/neon-city.png", floor: "#0a0522", sky: "rgba(54, 233, 255, .16)", horizon: "rgba(105, 55, 225, .28)", veil: "rgba(2, 8, 38, .18)", event: "comet" },
    { id: "aurora_rise", name: "AURORA RISE", price: 55, accent: "#61f5b3", background: "images/backgrounds/themes/polar-glow.png", floor: "#05251e", sky: "rgba(96, 255, 185, .16)", horizon: "rgba(43, 183, 147, .28)", veil: "rgba(1, 28, 25, .18)", event: "aurora" },
    { id: "solar_drift", name: "SOLAR DRIFT", price: 90, accent: "#ffc34d", background: "images/backgrounds/themes/amber-skies.png", floor: "#2b0d10", sky: "rgba(255, 195, 77, .14)", horizon: "rgba(242, 85, 54, .30)", veil: "rgba(45, 11, 8, .18)", event: "embers" },
    { id: "midnight_tide", name: "MIDNIGHT TIDE", price: 125, accent: "#45eaff", background: "images/backgrounds/themes/cobalt-storm.png", floor: "#07113d", sky: "rgba(73, 177, 255, .14)", horizon: "rgba(29, 69, 194, .28)", veil: "rgba(2, 10, 45, .22)", event: "rain" },
    { id: "velvet_dawn", name: "VELVET DAWN", price: 160, accent: "#f05bc6", background: "images/backgrounds/themes/rose-orbit.png", floor: "#26051f", sky: "rgba(255, 111, 207, .15)", horizon: "rgba(245, 83, 128, .30)", veil: "rgba(49, 5, 34, .18)", event: "spark" },
    { id: "crystal_night", name: "CRYSTAL NIGHT", price: 190, accent: "#edf7ff", background: "images/backgrounds/themes/crystal-night.png", floor: "#071239", sky: "rgba(210, 238, 255, .14)", horizon: "rgba(100, 129, 255, .27)", veil: "rgba(3, 14, 52, .20)", event: "spark" },
    { id: "jade_horizon", name: "JADE HORIZON", price: 215, accent: "#61f5b3", background: "images/backgrounds/themes/jade-horizon.png", floor: "#063523", sky: "rgba(89, 247, 172, .14)", horizon: "rgba(14, 135, 90, .30)", veil: "rgba(1, 35, 22, .19)", event: "aurora" },
    { id: "rose_orbit", name: "ROSE ORBIT", price: 240, accent: "#f05bc6", background: "images/backgrounds/themes/rose-orbit.png", floor: "#3d0925", sky: "rgba(255, 93, 185, .17)", horizon: "rgba(157, 45, 149, .31)", veil: "rgba(52, 4, 40, .19)", event: "spark" },
    { id: "cobalt_storm", name: "COBALT STORM", price: 265, accent: "#45eaff", background: "images/backgrounds/themes/cobalt-storm.png", floor: "#06153c", sky: "rgba(77, 183, 255, .18)", horizon: "rgba(27, 69, 214, .32)", veil: "rgba(2, 9, 43, .24)", event: "rain" },
    { id: "amber_skies", name: "AMBER SKIES", price: 290, accent: "#ffc34d", background: "images/backgrounds/themes/amber-skies.png", floor: "#3b1208", sky: "rgba(255, 198, 80, .18)", horizon: "rgba(255, 89, 45, .34)", veil: "rgba(53, 13, 4, .19)", event: "embers" },
    { id: "violet_rain", name: "VIOLET RAIN", price: 315, accent: "#b17cff", background: "images/backgrounds/themes/violet-rain.png", floor: "#210842", sky: "rgba(184, 124, 255, .16)", horizon: "rgba(115, 62, 228, .31)", veil: "rgba(25, 4, 55, .22)", event: "rain" },
    { id: "polar_glow", name: "POLAR GLOW", price: 340, accent: "#edf7ff", background: "images/backgrounds/themes/polar-glow.png", floor: "#073144", sky: "rgba(224, 249, 255, .17)", horizon: "rgba(70, 213, 217, .26)", veil: "rgba(2, 29, 47, .18)", event: "aurora" },
    { id: "eclipse", name: "ECLIPSE", price: 365, accent: "#b17cff", background: "images/backgrounds/themes/eclipse.png", floor: "#10051f", sky: "rgba(178, 111, 255, .13)", horizon: "rgba(74, 35, 122, .34)", veil: "rgba(13, 2, 28, .26)", event: "comet" },
  ];

  const trails = [
    { id: "pulse", name: "PULSE", price: 0, colours: ["#8f64ff", "#45eaff"] },
    { id: "solar", name: "SOLAR", price: 35, colours: ["#ffc34d", "#f05bc6"] },
    { id: "aurora", name: "AURORA", price: 60, colours: ["#61f5b3", "#8f64ff"] },
    { id: "comet", name: "COMET", price: 85, colours: ["#edf7ff", "#45eaff"] },
    { id: "ember", name: "EMBER", price: 115, colours: ["#f05bc6", "#ffc34d"] },
    { id: "nebula", name: "NEBULA", price: 140, colours: ["#8f64ff", "#f05bc6"] },
    { id: "mintwave", name: "MINTWAVE", price: 160, colours: ["#61f5b3", "#45eaff"] },
    { id: "sakura", name: "SAKURA", price: 180, colours: ["#f05bc6", "#edf7ff"] },
    { id: "glacial", name: "GLACIAL", price: 200, colours: ["#edf7ff", "#8f64ff"] },
    { id: "voltage", name: "VOLTAGE", price: 225, colours: ["#ffc34d", "#45eaff"] },
    { id: "cinder", name: "CINDER", price: 250, colours: ["#f05bc6", "#8f64ff"] },
    { id: "seaglass", name: "SEAGLASS", price: 275, colours: ["#61f5b3", "#edf7ff"] },
    { id: "starlight", name: "STARLIGHT", price: 300, colours: ["#edf7ff", "#ffc34d"] },
  ];

  const pipes = [
    { id: "ion", name: "ION", price: 0, accent: "#45eaff", panel: "#0b3076", energy: "#45eaff" },
    { id: "rose", name: "ROSE", price: 40, accent: "#f05bc6", panel: "#501144", energy: "#b17cff" },
    { id: "solar", name: "SOLAR", price: 70, accent: "#ffc34d", panel: "#592409", energy: "#f05bc6" },
    { id: "mint", name: "MINT", price: 105, accent: "#61f5b3", panel: "#0a442f", energy: "#45eaff" },
    { id: "prism", name: "PRISM", price: 145, accent: "#edf7ff", panel: "#2b1257", energy: "#f05bc6" },
    { id: "cobalt", name: "COBALT", price: 170, accent: "#45eaff", panel: "#102c80", energy: "#edf7ff" },
    { id: "jade", name: "JADE", price: 195, accent: "#61f5b3", panel: "#0b4827", energy: "#45eaff" },
    { id: "emberline", name: "EMBERLINE", price: 220, accent: "#f05bc6", panel: "#5b110e", energy: "#ffc34d" },
    { id: "amethyst_pipe", name: "AMETHYST", price: 245, accent: "#b17cff", panel: "#35115c", energy: "#edf7ff" },
    { id: "frost", name: "FROST", price: 270, accent: "#edf7ff", panel: "#183f60", energy: "#45eaff" },
    { id: "sunset", name: "SUNSET", price: 295, accent: "#ffc34d", panel: "#6a1b0a", energy: "#ffc34d" },
    { id: "seafoam", name: "SEAFOAM", price: 320, accent: "#61f5b3", panel: "#0b4a42", energy: "#edf7ff" },
    { id: "obsidian", name: "OBSIDIAN", price: 345, accent: "#edf7ff", panel: "#130d28", energy: "#f05bc6" },
  ];

  const catalog = { skins, themes, trails, pipes };
  const byId = (items) => Object.fromEntries(items.map((item) => [item.id, item]));
  const skinById = byId(skins);
  const themeById = byId(themes);
  const trailById = byId(trails);
  const pipeById = byId(pipes);
  const STORE_KEY = "skypulse-web-progress-v1";
  const BUILD = "0.4.2-beta";
  const milestoneRewards = [
    { id: "score-10", target: 10, category: "trails", rewardId: "solar" },
    { id: "score-25", target: 25, category: "pipes", rewardId: "rose" },
    { id: "score-50", target: 50, category: "skins", rewardId: "lumen" },
    { id: "score-75", target: 75, category: "themes", rewardId: "aurora_rise" },
    { id: "score-100", target: 100, category: "skins", rewardId: "aether" },
  ];

  function localDayKey() {
    const now = new Date();
    const month = String(now.getMonth() + 1).padStart(2, "0");
    const day = String(now.getDate()).padStart(2, "0");
    return `${now.getFullYear()}-${month}-${day}`;
  }

  function daySeed(day = localDayKey()) {
    return [...day].reduce((total, character) => total * 31 + character.charCodeAt(0), 17) >>> 0;
  }

  function emptyDaily() {
    return { date: localDayKey(), bestScore: 0, crests: 0, flights: 0, claimed: [], rewards: {} };
  }

  const fallbackProgress = {
    crystals: 0,
    best: 0,
    sound: true,
    reduceMotion: false,
    highContrast: false,
    hasSeenTutorial: false,
    daily: emptyDaily(),
    milestones: { claimed: [] },
    equipped: { skin: "nova", theme: "neon_city", trail: "pulse", pipe: "ion" },
    unlocked: { skins: ["nova"], themes: ["neon_city"], trails: ["pulse"], pipes: ["ion"] },
  };

  function readProgress() {
    try {
      const saved = JSON.parse(localStorage.getItem(STORE_KEY));
      if (!saved || typeof saved !== "object") return structuredClone(fallbackProgress);
      const result = structuredClone(fallbackProgress);
      result.crystals = Math.max(0, Number(saved.crystals) || 0);
      result.best = Math.max(0, Number(saved.best) || 0);
      result.sound = saved.sound !== false;
      result.reduceMotion = Boolean(saved.reduceMotion);
      result.highContrast = Boolean(saved.highContrast);
      result.hasSeenTutorial = Boolean(saved.hasSeenTutorial);
      const today = emptyDaily();
      if (saved.daily?.date === today.date) {
        result.daily.bestScore = Math.max(0, Number(saved.daily.bestScore) || 0);
        result.daily.crests = Math.max(0, Number(saved.daily.crests) || 0);
        result.daily.flights = Math.max(0, Number(saved.daily.flights) || 0);
        result.daily.claimed = Array.isArray(saved.daily.claimed) ? saved.daily.claimed.filter((item) => item === "daily") : [];
        result.daily.rewards = typeof saved.daily.rewards === "object" && saved.daily.rewards ? saved.daily.rewards : {};
      }
      result.milestones.claimed = Array.isArray(saved.milestones?.claimed) ? saved.milestones.claimed.filter((item) => milestoneRewards.some((milestone) => milestone.id === item)) : [];
      for (const key of Object.keys(result.equipped)) {
        const group = key === "skin" ? "skins" : `${key}s`;
        const available = catalog[group];
        const starter = result.unlocked[group][0];
        const owned = Array.isArray(saved.unlocked?.[group]) ? saved.unlocked[group].filter((id) => available.some((item) => item.id === id)) : [];
        result.unlocked[group] = [...new Set([starter, ...owned])];
        result.equipped[key] = result.unlocked[group].includes(saved.equipped?.[key]) ? saved.equipped[key] : starter;
      }
      return result;
    } catch {
      return structuredClone(fallbackProgress);
    }
  }

  const progress = readProgress();
  const imageCache = new Map();
  const audioPools = new Map();
  let worldRequest = 0;
  let lastHapticAt = 0;
  let audioWarmed = false;
  const audioPaths = {
    flap: "audio/flap.wav", score: "audio/score.wav", crash: "audio/crash.wav", crystal: "audio/crystal.wav", best: "audio/new-best.wav", unlock: "audio/unlock.wav",
  };
  let activeCategory = "skins";
  let mode = "menu";
  let lastFrame = performance.now();
  let toastTimer = 0;
  let width = 420;
  let height = 860;

  function dailyMissions() {
    const seed = daySeed(progress.daily.date);
    const categories = ["trails", "pipes", "themes"];
    const target = 8 + seed % 5;
    return [
      { id: "daily", title: "SKYLINE RUN", target, category: categories[(seed >>> 4) % categories.length], detail: (value) => `Today’s shared route — reach score ${value}.` },
    ];
  }

  function dailyValue(mission) {
    return progress.daily.bestScore;
  }

  function rewardFor(mission, index) {
    const choices = catalog[mission.category].filter((item) => item.price > 0);
    const stored = choices.find((item) => item.id === progress.daily.rewards[mission.id]);
    if (stored && !progress.unlocked[mission.category].includes(stored.id)) return stored;
    const seed = daySeed(progress.daily.date) + index * 7;
    const available = choices.filter((item) => !progress.unlocked[mission.category].includes(item.id));
    const reward = (available.length ? available : choices)[seed % (available.length || choices.length)];
    progress.daily.rewards[mission.id] = reward.id;
    return reward;
  }

  function settleDailyRewards() {
    const unlocked = [];
    dailyMissions().forEach((mission, index) => {
      if (dailyValue(mission) < mission.target || progress.daily.claimed.includes(mission.id)) return;
      const reward = rewardFor(mission, index);
      progress.daily.claimed.push(mission.id);
      if (!progress.unlocked[mission.category].includes(reward.id)) {
        progress.unlocked[mission.category].push(reward.id);
        unlocked.push(reward);
      }
    });
    return unlocked;
  }

  function milestoneReward(milestone) {
    const choices = catalog[milestone.category].filter((item) => item.price > 0);
    const preferred = choices.find((item) => item.id === milestone.rewardId);
    if (preferred && !progress.unlocked[milestone.category].includes(preferred.id)) return preferred;
    return choices.find((item) => !progress.unlocked[milestone.category].includes(item.id));
  }

  function settleMilestoneRewards() {
    const unlocked = [];
    for (const milestone of milestoneRewards) {
      if (progress.best < milestone.target || progress.milestones.claimed.includes(milestone.id)) continue;
      progress.milestones.claimed.push(milestone.id);
      const reward = milestoneReward(milestone);
      if (!reward) continue;
      progress.unlocked[milestone.category].push(reward.id);
      unlocked.push({ ...reward, target: milestone.target });
    }
    return unlocked;
  }

  function renderDaily() {
    const date = new Date(`${progress.daily.date}T12:00:00`);
    ui.dailyDate.textContent = new Intl.DateTimeFormat(undefined, { month: "short", day: "numeric" }).format(date).toUpperCase();
    ui.dailyList.replaceChildren(...dailyMissions().map((mission, index) => {
      const reward = rewardFor(mission, index);
      const value = Math.min(mission.target, dailyValue(mission));
      const complete = progress.daily.claimed.includes(mission.id);
      const card = document.createElement("article");
      card.className = `daily-card${complete ? " complete" : ""}`;
      card.innerHTML = `<h3>${mission.title}</h3><p>${mission.detail(mission.target)}</p><footer><span>${value} / ${mission.target}</span><span>${complete ? "UNLOCKED" : `REWARD · ${reward.name}`}</span></footer>`;
      return card;
    }));
  }

  const flight = {
    bird: { x: 0, y: 0, velocity: 0, tilt: 0, wing: 0, wingPhase: 0 },
    pipes: [],
    crests: [],
    trail: [],
    trailTimer: 0,
    bursts: [],
    score: 0,
    earned: 0,
    spawnTimer: 1.15,
    runningTime: 0,
    tutorialActive: false,
    hintTimer: 0,
    impactAt: 0,
    impactType: "",
    dailyRun: false,
    routeState: 0,
    perfectPasses: 0,
    newBestUntil: 0,
    comet: null,
    worldEventTimer: 6,
  };

  function saveProgress() {
    try { localStorage.setItem(STORE_KEY, JSON.stringify(progress)); } catch { /* private browsing can still play */ }
  }

  function image(relativePath) {
    const path = ASSET + relativePath;
    if (imageCache.has(path)) return imageCache.get(path);
    const item = new Image();
    item.decoding = "async";
    item.src = path;
    imageCache.set(path, item);
    return item;
  }

  function audioPool(name) {
    if (audioPools.has(name)) return audioPools.get(name);
    const voices = name === "flap" ? 3 : name === "score" ? 2 : 1;
    const pool = Array.from({ length: voices }, () => {
      const sound = new Audio(ASSET + audioPaths[name]);
      sound.preload = "auto";
      sound.volume = name === "crash" ? .45 : .32;
      return sound;
    });
    audioPools.set(name, pool);
    return pool;
  }

  function warmFlightAssets() {
    const skin = currentSkin();
    image(skin.art);
    image(skin.flap);
    if (progress.sound && !audioWarmed) {
      audioWarmed = true;
      for (const name of ["flap", "score", "crash", "crystal", "best", "unlock"]) {
        for (const sound of audioPool(name)) sound.load();
      }
    }
  }

  function playSound(name) {
    if (!progress.sound || !audioPaths[name]) return;
    try {
      const pool = audioPool(name);
      const sound = pool.find((voice) => voice.paused || voice.ended) || pool[0];
      sound.currentTime = 0;
      sound.play().catch(() => {});
    } catch { /* audio is optional */ }
  }

  function haptic(pattern, cooldown = 0) {
    if (!navigator.vibrate) return;
    const now = performance.now();
    if (now - lastHapticAt < cooldown) return;
    lastHapticAt = now;
    navigator.vibrate(pattern);
  }

  function currentSkin() { return skinById[progress.equipped.skin]; }
  function currentTheme() { return themeById[progress.equipped.theme]; }
  function currentTrail() { return trailById[progress.equipped.trail]; }
  function currentPipe() { return pipeById[progress.equipped.pipe]; }
  function previewArt(relativePath) { return relativePath.replace("images/characters/", "images/characters/thumbs/"); }

  function updateWorldBackground() {
    const theme = currentTheme();
    const source = ASSET + theme.background;
    ui.app.style.setProperty("--theme-accent", theme.accent);
    ui.app.style.setProperty("--theme-accent-soft", `${theme.accent}24`);
    ui.app.style.setProperty("--world-sky", theme.sky);
    ui.app.style.setProperty("--world-horizon", theme.horizon);
    ui.app.style.setProperty("--world-veil", theme.veil);
    ui.app.dataset.world = theme.id;
    const request = ++worldRequest;
    const apply = () => {
      if (request !== worldRequest) return;
      ui.world.style.setProperty("--world-image", `url("${source}")`);
    };
    const backdrop = image(theme.background);
    if (backdrop.complete && backdrop.naturalWidth) apply();
    else backdrop.addEventListener("load", apply, { once: true });
  }

  function resize() {
    const rect = canvas.getBoundingClientRect();
    const dpr = Math.min(window.devicePixelRatio || 1, 1.35);
    width = Math.max(1, rect.width);
    height = Math.max(1, rect.height);
    canvas.width = Math.round(width * dpr);
    canvas.height = Math.round(height * dpr);
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.imageSmoothingQuality = "medium";
    if (mode !== "playing") resetFlight();
  }

  function setVisible(element, visible) { element.classList.toggle("hidden", !visible); }

  function renderUi() {
    const skin = currentSkin();
    const theme = currentTheme();
    updateWorldBackground();
    warmFlightAssets();
    ui.menu.style.setProperty("--skin-aura", `${skin.accent}38`);
    ui.menu.style.setProperty("--skin-orbit", `${skin.trail[1]}a6`);
    ui.menu.style.setProperty("--skin-accent", skin.accent);
    ui.menu.style.setProperty("--world-highlight", theme.horizon);
    ui.crystals.textContent = `✦ ${progress.crystals}`;
    ui.menuCurrency.textContent = `✦ ${progress.crystals}`;
    ui.shopCurrency.textContent = `✦ ${progress.crystals}`;
    ui.best.textContent = `BEST · ${progress.best}`;
    ui.menuBird.src = ASSET + skin.art;
    ui.equipped.textContent = `${skin.name} EQUIPPED`;
    ui.sound.textContent = `SOUND  ${progress.sound ? "ON" : "OFF"}`;
    ui.motion.textContent = `REDUCED MOTION  ${progress.reduceMotion ? "ON" : "OFF"}`;
    ui.contrast.textContent = `HIGH CONTRAST  ${progress.highContrast ? "ON" : "OFF"}`;
    ui.app.classList.toggle("high-contrast", progress.highContrast);
    setVisible(ui.menu, mode === "menu");
    setVisible(ui.hud, mode === "playing" || mode === "newbest");
    setVisible(ui.customize, mode === "shop");
    setVisible(ui.daily, mode === "daily");
    setVisible(ui.gameOver, mode === "gameover");
    setVisible(ui.pause, mode === "paused");
    setVisible(ui.settings, mode === "settings");
  }

  function toast(message) {
    ui.toast.textContent = message;
    setVisible(ui.toast, true);
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => setVisible(ui.toast, false), 1800);
  }

  function setFlightHint(message) {
    ui.flightHint.textContent = message;
  }

  function showFlightHint(message, duration = 0) {
    clearTimeout(flight.hintTimer);
    setFlightHint(message);
    if (duration) {
      flight.hintTimer = setTimeout(() => {
        if (mode === "playing") setFlightHint("");
      }, duration);
    }
  }

  function resetFlight(dailyRun = false) {
    const bird = flight.bird;
    bird.x = width * .31;
    bird.y = height * .52;
    bird.velocity = 0;
    bird.tilt = 0;
    bird.wing = 0;
    bird.wingPhase = 0;
    flight.pipes = [];
    flight.crests = [];
    flight.trail = [];
    flight.bursts = [];
    flight.score = 0;
    flight.earned = 0;
    flight.spawnTimer = .82;
    flight.runningTime = 0;
    flight.impactAt = 0;
    flight.impactType = "";
    flight.trailTimer = 0;
    flight.dailyRun = dailyRun;
    flight.routeState = (daySeed(progress.daily.date) ^ 0x9e3779b9) >>> 0;
    flight.perfectPasses = 0;
    flight.newBestUntil = 0;
    flight.comet = null;
    flight.worldEventTimer = 5.8 + Math.random() * 3;
    ui.hud.classList.remove("new-best-flash");
  }

  function startFlight(dailyRun = false) {
    resetFlight(dailyRun);
    mode = "playing";
    flight.tutorialActive = !progress.hasSeenTutorial;
    const daily = dailyMissions()[0];
    showFlightHint(dailyRun ? `DAILY ROUTE · TARGET ${daily.target}` : flight.tutorialActive ? "TAP TO FLAP · FLY THROUGH THE GAPS" : "");
    flap();
    renderUi();
  }

  function flap() {
    if (mode !== "playing") return;
    const bird = flight.bird;
    bird.velocity = -390;
    bird.wing = 1;
    playSound("flap");
  }

  function addPipe() {
    const ground = height * .88;
    const gap = Math.max(height * .198, height * .267 - Math.min(flight.score, 25) * 1.25);
    const min = height * .17 + gap / 2;
    const max = ground - gap / 2 - height * .05;
    const gapY = min + nextRouteRandom() * Math.max(1, max - min);
    flight.pipes.push({ x: width + 35, gapY, gap, passed: false });
    if (nextRouteRandom() < .61) flight.crests.push({ x: width + 78, y: gapY + (nextRouteRandom() - .5) * gap * .30, phase: nextRouteRandom() * Math.PI * 2 });
  }

  function nextRouteRandom() {
    if (!flight.dailyRun) return Math.random();
    flight.routeState = (flight.routeState * 1664525 + 1013904223) >>> 0;
    return flight.routeState / 4294967296;
  }

  function endFlight(impactType = "pipe") {
    if (mode !== "playing") return;
    const previousBest = progress.best;
    const newBest = flight.score > previousBest && flight.score > 0;
    progress.daily.bestScore = Math.max(progress.daily.bestScore, flight.score);
    progress.daily.flights += 1;
    progress.best = Math.max(progress.best, flight.score);
    const dailyUnlocks = settleDailyRewards();
    const milestoneUnlocks = settleMilestoneRewards();
    flight.impactAt = performance.now();
    flight.impactType = impactType;
    saveProgress();
    playSound("crash");
    if (newBest) playSound("best");
    if (dailyUnlocks.length || milestoneUnlocks.length) playSound("unlock");
    haptic([16, 36, 26], 130);
    ui.resultScore.textContent = `SCORE  ${flight.score}`;
    ui.resultBest.textContent = newBest ? `NEW BEST  ${progress.best}` : `BEST  ${progress.best}`;
    ui.resultCrystals.textContent = `CRESTS  +${flight.earned}`;
    ui.gameOver.style.setProperty("--result-accent", currentSkin().accent);
    ui.hud.style.setProperty("--result-accent", currentSkin().accent);
    ui.gameOver.classList.toggle("new-best", newBest);
    setVisible(ui.resultCelebration, newBest);
    const medal = perfectFlightMedal();
    ui.resultMedal.textContent = medal;
    setVisible(ui.resultMedal, Boolean(medal));
    const dailyComplete = progress.daily.claimed.length;
    if (dailyUnlocks.length) {
      ui.resultDaily.textContent = `DAILY UNLOCKED · ${dailyUnlocks.map((item) => item.name).join(" · ")}`;
    } else {
      ui.resultDaily.textContent = `DAILY SKYLINE RUN · ${dailyComplete} / 1 COMPLETE`;
    }
    setVisible(ui.resultDaily, true);
    if (milestoneUnlocks.length) {
      ui.resultMilestone.textContent = `SCORE ${milestoneUnlocks.map((item) => item.target).join(" + ")} UNLOCKED · ${milestoneUnlocks.map((item) => item.name).join(" · ")}`;
      setVisible(ui.resultMilestone, true);
    } else {
      setVisible(ui.resultMilestone, false);
    }
    setFlightHint("");
    if (newBest) {
      mode = "newbest";
      flight.newBestUntil = performance.now() + 360;
      ui.hud.classList.add("new-best-flash");
      setFlightHint("NEW BEST");
    } else {
      mode = "gameover";
      renderUi();
    }
  }

  function perfectFlightMedal() {
    if (flight.score >= 10 && flight.perfectPasses >= 10) return "CENTERLINE · 10 PERFECT GAPS";
    return "";
  }

  function update(dt) {
    if (mode !== "playing") return;
    const bird = flight.bird;
    const ground = height * .88;
    const speed = width * (.57 + Math.min(flight.score, 25) * .007);
    flight.runningTime += dt;
    if (!progress.reduceMotion) {
      flight.worldEventTimer -= dt;
      if (flight.comet) {
        flight.comet.age += dt;
        if (flight.comet.age >= .92) flight.comet = null;
      }
      if (flight.runningTime > 5 && flight.worldEventTimer <= 0) {
        flight.comet = { age: 0, y: height * (.16 + Math.random() * .30) };
        flight.worldEventTimer = 9 + Math.random() * 8;
      }
    }
    bird.wing = Math.max(0, bird.wing - dt * 3.8);
    bird.wingPhase += dt * (bird.velocity < 0 ? 16 : 8);
    bird.velocity = Math.min(730, bird.velocity + 1070 * dt);
    bird.y += bird.velocity * dt;
    const targetTilt = Math.max(-30, Math.min(24, bird.velocity * .072));
    bird.tilt += (targetTilt - bird.tilt) * Math.min(1, dt * (targetTilt > bird.tilt ? 12 : 5.5));
    for (let index = flight.trail.length - 1; index >= 0; index -= 1) {
      flight.trail[index].age += dt;
      if (flight.trail[index].age >= .46) flight.trail.splice(index, 1);
    }
    flight.trailTimer -= dt;
    if (flight.trailTimer <= 0) {
      flight.trail.unshift({ x: bird.x - 27, y: bird.y, age: 0 });
      if (flight.trail.length > 14) flight.trail.pop();
      flight.trailTimer += 1 / 30;
    }
    flight.spawnTimer -= dt;
    if (flight.spawnTimer <= 0) {
      addPipe();
      flight.spawnTimer = 1.48;
    }
    const pipeWidth = Math.max(62, width * .16);
    // Keep the generous visual silhouette, but judge contact from its compact inner body.
    const bodyHalfW = 17;
    const bodyHalfH = 13;
    for (const pipe of flight.pipes) {
      pipe.x -= speed * dt;
      if (!pipe.passed && pipe.x + pipeWidth < bird.x - bodyHalfW) {
        pipe.passed = true;
        flight.score += 1;
        if (Math.abs(bird.y - pipe.gapY) <= Math.min(22, pipe.gap * .13)) flight.perfectPasses += 1;
        flight.bursts.push({ x: bird.x + 24, y: bird.y + 23, age: 0, colour: currentSkin().accent });
        playSound("score");
        if (flight.tutorialActive) {
          flight.tutorialActive = false;
          progress.hasSeenTutorial = true;
          saveProgress();
          showFlightHint("NICE FLIGHT", 1100);
        }
      }
      const overlapsX = bird.x + bodyHalfW > pipe.x - 6 && bird.x - bodyHalfW < pipe.x + pipeWidth + 6;
      const dangerousTop = pipe.gapY - pipe.gap / 2 + 3;
      const dangerousBottom = pipe.gapY + pipe.gap / 2 - 4;
      if (overlapsX && (bird.y - bodyHalfH < dangerousTop || bird.y + bodyHalfH > dangerousBottom)) endFlight("pipe");
    }
    for (let index = flight.pipes.length - 1; index >= 0; index -= 1) {
      if (flight.pipes[index].x + pipeWidth <= -50) flight.pipes.splice(index, 1);
    }
    for (const crest of flight.crests) {
      crest.x -= speed * dt;
      crest.phase += dt * 3;
      if (Math.hypot(crest.x - bird.x, crest.y - bird.y) < 32) {
        crest.x = -100;
        progress.crystals += 1;
        progress.daily.crests += 1;
        flight.earned += 1;
        saveProgress();
        playSound("crystal");
      }
    }
    for (let index = flight.crests.length - 1; index >= 0; index -= 1) {
      if (flight.crests[index].x <= -40) flight.crests.splice(index, 1);
    }
    for (let index = flight.bursts.length - 1; index >= 0; index -= 1) {
      flight.bursts[index].age += dt;
      if (flight.bursts[index].age >= .66) flight.bursts.splice(index, 1);
    }
    if (bird.y < -20) endFlight("sky");
    if (bird.y + bodyHalfH >= ground) endFlight("floor");
  }

  function roundedRect(x, y, w, h, radius) {
    const r = Math.min(radius, w / 2, h / 2);
    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.arcTo(x + w, y, x + w, y + h, r);
    ctx.arcTo(x + w, y + h, x, y + h, r);
    ctx.arcTo(x, y + h, x, y, r);
    ctx.arcTo(x, y, x + w, y, r);
    ctx.closePath();
  }

  function drawBackground() {
    const theme = currentTheme();
    if (mode === "playing" && !progress.reduceMotion) {
      ctx.save();
      ctx.fillStyle = `${theme.accent}18`;
      ctx.fillRect(0, height * .745, width, 1);
      for (let index = 0; index < 9; index += 1) {
        const x = (index * 71 + flight.runningTime * (4 + index % 3)) % (width + 18) - 9;
        const y = height * (.18 + (index * .067) % .54);
        const size = index % 4 === 0 ? 2 : 1;
        ctx.globalAlpha = index % 3 === 0 ? .82 : .48;
        ctx.fillStyle = index % 2 ? theme.accent : "#eef9ff";
        ctx.fillRect(x, y, size, size);
      }
      ctx.restore();
      if (flight.comet) drawWorldEvent(theme);
    }
  }

  function drawWorldEvent(theme) {
    const progressValue = flight.comet.age / .92;
    const x = width * (1.12 - progressValue * 1.34);
    const y = flight.comet.y + progressValue * height * .16;
    ctx.save();
    const alpha = Math.sin(progressValue * Math.PI) * .78;
    ctx.globalAlpha = alpha;
    ctx.lineCap = "round";
    if (theme.event === "aurora") {
      ctx.strokeStyle = theme.accent;
      ctx.lineWidth = 1.4;
      for (let strand = 0; strand < 3; strand += 1) {
        const offset = strand * 16;
        ctx.globalAlpha = alpha * (.17 + strand * .07);
        ctx.beginPath();
        ctx.moveTo(-10, y + offset);
        ctx.bezierCurveTo(width * .24, y - 28 + offset, width * .63, y + 37 - offset, width + 12, y + 6 + offset);
        ctx.stroke();
      }
    } else if (theme.event === "rain") {
      ctx.strokeStyle = theme.accent;
      ctx.lineWidth = 1;
      for (let streak = 0; streak < 6; streak += 1) {
        const streakX = (x + streak * 53) % (width + 18) - 9;
        const streakY = y - 48 + streak * 17;
        ctx.beginPath();
        ctx.moveTo(streakX + 7, streakY - 13);
        ctx.lineTo(streakX, streakY + 8);
        ctx.stroke();
      }
    } else if (theme.event === "embers" || theme.event === "spark") {
      ctx.fillStyle = theme.event === "embers" ? "#ffc34d" : theme.accent;
      for (let spark = 0; spark < 7; spark += 1) {
        const sparkX = (x + spark * 31) % (width + 14) - 7;
        const sparkY = y - 20 + Math.sin(progressValue * 7 + spark) * 22 + spark * 5;
        const size = spark % 3 === 0 ? 2 : 1;
        ctx.fillRect(sparkX, sparkY, size, size);
      }
    } else {
      ctx.strokeStyle = theme.accent;
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      ctx.moveTo(x + 56, y - 25);
      ctx.lineTo(x, y);
      ctx.stroke();
      ctx.fillStyle = "#f4fbff";
      ctx.beginPath();
      ctx.arc(x, y, 2.2, 0, Math.PI * 2);
      ctx.fill();
    }
    ctx.restore();
  }

  function drawFloor() {
    const theme = currentTheme();
    const floorY = height * .88;
    const runningTime = mode === "playing" ? flight.runningTime : 0;
    ctx.fillStyle = "#02030d";
    ctx.fillRect(0, floorY, width, height - floorY);
    ctx.fillStyle = theme.floor;
    ctx.fillRect(0, floorY + 7, width, height - floorY - 7);
    ctx.fillStyle = `${theme.accent}4a`;
    ctx.fillRect(0, floorY, width, 10);
    ctx.fillStyle = "rgba(255,255,255,.18)";
    ctx.fillRect(0, floorY + 2, width, 1.2);
    ctx.fillStyle = "rgba(0,0,0,.36)";
    ctx.fillRect(0, floorY + 10, width, 4);
    ctx.strokeStyle = `${theme.accent}bb`;
    ctx.lineWidth = 2.25;
    ctx.beginPath();
    ctx.moveTo(0, floorY + 1);
    ctx.lineTo(width, floorY + 1);
    ctx.stroke();
    ctx.strokeStyle = `${theme.accent}33`;
    ctx.lineWidth = 1;
    const laneEnds = [width * .08, width * .28, width * .5, width * .72, width * .92];
    for (const endX of laneEnds) {
      ctx.beginPath();
      ctx.moveTo(width * .5 + (endX - width * .5) * .16, floorY + 7);
      ctx.lineTo(endX, height);
      ctx.stroke();
    }
    const firstGridLine = floorY + 14 + (runningTime * 38) % 19;
    for (let y = firstGridLine; y < height; y += 19) {
      const depth = (y - floorY) / Math.max(1, height - floorY);
      ctx.globalAlpha = .16 + depth * .30;
      ctx.beginPath();
      ctx.moveTo(0, y);
      ctx.lineTo(width, y);
      ctx.stroke();
    }
    ctx.globalAlpha = 1;
  }

  function drawPipe(pipe) {
    const pipeStyle = currentPipe();
    const floorY = height * .88;
    const pipeWidth = Math.max(62, width * .16);
    const sections = [[0, pipe.gapY - pipe.gap / 2], [pipe.gapY + pipe.gap / 2, floorY - (pipe.gapY + pipe.gap / 2)]];
    for (const [y, h] of sections) {
      if (h <= 0) continue;
      ctx.fillStyle = "#030613";
      roundedRect(pipe.x, y, pipeWidth, h, 7);
      ctx.fill();
      ctx.fillStyle = pipeStyle.panel;
      roundedRect(pipe.x + 8, y + 4, pipeWidth - 16, Math.max(0, h - 8), 4);
      ctx.fill();
      ctx.fillStyle = "rgba(0,0,0,.23)";
      ctx.fillRect(pipe.x + pipeWidth - 16, y + 7, 7, Math.max(0, h - 14));
      ctx.fillStyle = `${pipeStyle.accent}24`;
      ctx.fillRect(pipe.x + 11, y + 7, 2, Math.max(0, h - 14));
      ctx.fillStyle = "rgba(255,255,255,.13)";
      ctx.fillRect(pipe.x + 14, y + 7, Math.max(0, pipeWidth - 30), 1.2);
      ctx.fillStyle = pipeStyle.energy;
      ctx.fillRect(pipe.x + pipeWidth / 2 - 1.5, y + 7, 3, Math.max(0, h - 14));
      ctx.fillStyle = `${pipeStyle.energy}30`;
      for (let stripeY = y + 22; stripeY < y + h - 12; stripeY += 29) ctx.fillRect(pipe.x + 17, stripeY, Math.max(0, pipeWidth - 34), 1);
      ctx.strokeStyle = pipeStyle.accent;
      ctx.globalAlpha = .82;
      ctx.lineWidth = 1.4;
      ctx.strokeRect(pipe.x + .7, y + .7, pipeWidth - 1.4, h - 1.4);
      ctx.globalAlpha = 1;
    }
    for (const y of [pipe.gapY - pipe.gap / 2 - 11, pipe.gapY + pipe.gap / 2 - 4]) {
      ctx.fillStyle = "#030613";
      roundedRect(pipe.x - 7, y - 1, pipeWidth + 14, 16, 5);
      ctx.fill();
      ctx.fillStyle = pipeStyle.accent;
      roundedRect(pipe.x - 5, y + 1, pipeWidth + 10, 12, 4);
      ctx.fill();
      ctx.fillStyle = pipeStyle.panel;
      roundedRect(pipe.x - 2, y + 4, pipeWidth + 4, 6, 3);
      ctx.fill();
      ctx.fillStyle = pipeStyle.energy;
      ctx.fillRect(pipe.x + pipeWidth / 2 - 1.5, y + 4, 3, 6);
      ctx.strokeStyle = "rgba(255,255,255,.58)";
      ctx.lineWidth = 1;
      ctx.beginPath();
      ctx.moveTo(pipe.x - 2, y + 10);
      ctx.lineTo(pipe.x + pipeWidth + 2, y + 10);
      ctx.stroke();
    }
  }

  function drawCrest(crest) {
    const skin = currentSkin();
    const size = 16;
    ctx.save();
    ctx.translate(crest.x, crest.y + Math.sin(crest.phase) * 4);
    ctx.strokeStyle = skin.trail[1];
    ctx.lineWidth = 3.1;
    ctx.lineCap = "round";
    ctx.shadowColor = skin.accent;
    ctx.shadowBlur = 10;
    ctx.beginPath();
    ctx.moveTo(-size, -2); ctx.lineTo(-7, 9); ctx.lineTo(-1, 3); ctx.lineTo(4, 12); ctx.lineTo(size, -2);
    ctx.stroke();
    ctx.strokeStyle = skin.trail[0];
    ctx.lineWidth = 1.2;
    ctx.beginPath();
    ctx.moveTo(-13, -5); ctx.lineTo(-6, 4); ctx.lineTo(-1, -1); ctx.lineTo(4, 7); ctx.lineTo(13, -5);
    ctx.stroke();
    ctx.restore();
  }

  function drawTrail() {
    const points = flight.trail;
    if (points.length < 2) return;
    const trail = currentTrail();
    for (let index = 0; index < trail.colours.length; index += 1) {
      const colour = trail.colours[index];
      ctx.save();
      ctx.strokeStyle = colour;
      ctx.globalAlpha = index ? .42 : .78;
      ctx.lineWidth = index ? 7 : 3;
      ctx.lineCap = "round";
      ctx.beginPath();
      for (let pointIndex = 0; pointIndex < points.length; pointIndex += 1) {
        const point = points[pointIndex];
        const x = point.x - point.age * 132;
        const verticalBend = Math.max(-34, Math.min(34, (point.y - flight.bird.y) * .28));
        const y = flight.bird.y + verticalBend + Math.sin(point.age * 16 + index * 2) * (index ? 3 : 1);
        if (pointIndex === 0) ctx.moveTo(x, y); else ctx.lineTo(x, y);
      }
      ctx.stroke();
      ctx.restore();
    }
    ctx.save();
    ctx.fillStyle = trail.colours[0];
    for (let pointIndex = 2; pointIndex < points.length; pointIndex += 4) {
      const point = points[pointIndex];
      const life = Math.max(0, 1 - point.age / .46);
      const verticalBend = Math.max(-34, Math.min(34, (point.y - flight.bird.y) * .28));
      ctx.globalAlpha = life * .5;
      ctx.beginPath();
      ctx.arc(point.x - point.age * 142, flight.bird.y + verticalBend + Math.sin(point.age * 19) * 2, pointIndex % 8 === 2 ? 1.7 : 1.1, 0, Math.PI * 2);
      ctx.fill();
    }
    ctx.restore();
  }

  function drawBird() {
    const bird = flight.bird;
    const skin = currentSkin();
    const base = image(skin.art);
    const flapFrame = image(skin.flap);
    const idleWing = .12 + (Math.sin(bird.wingPhase) + 1) * .09;
    const wingMix = mode === "playing" ? Math.max(0, Math.min(1, idleWing + bird.wing * .76)) : .42 + Math.sin(performance.now() * .005) * .18;
    const artWidth = width * .30 * (skin.scale || 1);
    ctx.save();
    const lifeBob = mode === "playing" ? Math.sin(bird.wingPhase * .48) * .8 : Math.sin(performance.now() * .005) * 1.5;
    ctx.translate(bird.x, bird.y + lifeBob);
    ctx.rotate(bird.tilt * Math.PI / 180);
    const wingLift = wingMix * .035;
    ctx.scale(1 + wingLift, 1 - wingLift * .72);
    ctx.globalAlpha = 1 - wingMix;
    drawBirdFrame(base, artWidth, skin.tint);
    if (wingMix > .01) {
      ctx.globalAlpha = wingMix;
      drawBirdFrame(flapFrame, artWidth, skin.tint);
    }
    ctx.restore();
  }

  function drawBirdFrame(frame, artWidth, tint) {
    if (!frame.complete || !frame.naturalWidth) {
      ctx.fillStyle = "#52efff";
      ctx.beginPath(); ctx.ellipse(0, 0, artWidth * .35, artWidth * .23, 0, 0, Math.PI * 2); ctx.fill();
      return;
    }
    const artHeight = artWidth * frame.naturalHeight / frame.naturalWidth;
    ctx.drawImage(frame, -artWidth * .60, -artHeight * .50, artWidth, artHeight);
    if (tint) {
      const frameOpacity = ctx.globalAlpha;
      ctx.globalCompositeOperation = "source-atop";
      ctx.fillStyle = tint;
      ctx.globalAlpha = frameOpacity * .22;
      ctx.fillRect(-artWidth * .60, -artHeight * .50, artWidth, artHeight);
      ctx.globalCompositeOperation = "source-over";
      ctx.globalAlpha = frameOpacity;
    }
  }

  function drawBursts() {
    for (const burst of flight.bursts) {
      ctx.save();
      ctx.globalAlpha = Math.max(0, 1 - burst.age / .66);
      ctx.fillStyle = burst.colour;
      ctx.font = `800 ${15 + burst.age * 8}px system-ui`;
      ctx.textAlign = "center";
      ctx.fillText("+1", burst.x, burst.y - burst.age * 46);
      ctx.restore();
    }
  }

  function drawImpact() {
    if (!flight.impactAt) return;
    const age = (performance.now() - flight.impactAt) / 1000;
    if (age > .34) return;
    const alpha = (1 - age / .34) * .34;
    const colour = currentSkin().accent;
    ctx.save();
    ctx.fillStyle = colour;
    ctx.globalAlpha = alpha;
    if (flight.impactType === "floor") {
      const y = height * .88;
      ctx.fillRect(0, y - 4 - age * 32, width, 8 + age * 56);
    } else {
      ctx.beginPath();
      ctx.arc(flight.bird.x, flight.bird.y, 15 + age * 80, 0, Math.PI * 2);
      ctx.fill();
    }
    ctx.restore();
  }

  function draw() {
    ctx.clearRect(0, 0, width, height);
    drawBackground();
    if (mode === "playing" || mode === "paused" || mode === "gameover" || mode === "newbest") {
      for (const pipe of flight.pipes) drawPipe(pipe);
      for (const crest of flight.crests) drawCrest(crest);
      drawTrail();
      drawBird();
      drawBursts();
      drawFloor();
      drawImpact();
    }
    if (mode === "playing") ui.score.textContent = flight.score;
  }

  function tick(now) {
    const dt = Math.min(.034, Math.max(0, (now - lastFrame) / 1000));
    lastFrame = now;
    if (mode === "newbest" && now >= flight.newBestUntil) {
      mode = "gameover";
      ui.hud.classList.remove("new-best-flash");
      renderUi();
    }
    update(dt);
    draw();
    requestAnimationFrame(tick);
  }

  function showShop(category = activeCategory) {
    activeCategory = category;
    const items = catalog[category];
    ui.collection.textContent = `COLLECTION · ${progress.unlocked[category].length} / ${items.length}`;
    for (const button of ui.shopTabs.querySelectorAll("button")) button.classList.toggle("active", button.dataset.category === category);
    ui.shopGrid.replaceChildren(...items.map((item) => makeShopCard(category, item)));
  }

  function makeShopCard(category, item) {
    const unlocked = progress.unlocked[category].includes(item.id);
    const equippedKey = category === "skins" ? "skin" : category.slice(0, -1);
    const equipped = progress.equipped[equippedKey] === item.id;
    const card = document.createElement("button");
    card.className = `shop-card ${unlocked ? "owned" : "locked"} ${equipped ? "equipped" : ""}`;
    card.type = "button";
    let preview = "";
    if (category === "skins") {
      const generated = item.art.includes("/generated/") ? " generated" : "";
      preview = `<div class="shop-preview skin-preview"><img class="shop-skin-art${generated}" src="${ASSET + previewArt(item.art)}" alt="${item.name} bird" decoding="async"></div>`;
    }
    if (category === "themes") preview = `<div class="shop-preview"><span class="theme-preview" style="--theme-glow:${item.accent}88;background-image:url('${ASSET + item.background}')"></span></div>`;
    if (category === "trails") preview = `<div class="shop-preview"><span class="trail-preview" style="color:${item.colours[0]};background:${item.colours[1]}"></span></div>`;
    if (category === "pipes") preview = `<div class="shop-preview"><span class="pipe-preview" style="color:${item.accent};--panel:${item.panel};--energy:${item.energy}"></span></div>`;
    const status = equipped ? "EQUIPPED" : unlocked ? "TAP TO EQUIP" : `✦ ${item.price}`;
    card.innerHTML = `${preview}<strong class="shop-name">${item.name}</strong><span class="shop-status">${status}</span>`;
    card.addEventListener("click", () => selectItem(category, item));
    return card;
  }

  function selectItem(category, item) {
    const unlocked = progress.unlocked[category];
    const equippedKey = category === "skins" ? "skin" : category.slice(0, -1);
    if (!unlocked.includes(item.id)) {
      if (progress.crystals < item.price) {
        toast(`COLLECT ${item.price - progress.crystals} MORE CRESTS`);
        return;
      }
      progress.crystals -= item.price;
      unlocked.push(item.id);
      toast(`${item.name} UNLOCKED`);
    } else {
      toast(`${item.name} EQUIPPED`);
    }
    progress.equipped[equippedKey] = item.id;
    saveProgress();
    renderUi();
    showShop(category);
  }

  function goMenu() {
    mode = "menu";
    resetFlight();
    setFlightHint("");
    renderUi();
  }

  function feedbackText() {
    const completed = progress.daily.claimed.length;
    return [
      `SkyPulse ${BUILD} feedback`,
      `Best score: ${progress.best}`,
      `Daily route: ${completed}/1`,
      "Touch felt: ",
      "Any lag or glitches: ",
      "One thing you loved: ",
      "One thing to improve: ",
    ].join("\n");
  }

  async function shareFeedback() {
    const message = feedbackText();
    try {
      if (navigator.share) {
        await navigator.share({ title: "SkyPulse beta feedback", text: message });
        return;
      }
      if (navigator.clipboard?.writeText) {
        await navigator.clipboard.writeText(message);
        toast("FEEDBACK TEMPLATE COPIED");
        return;
      }
    } catch (error) {
      if (error?.name === "AbortError") return;
    }
    window.prompt("Copy your SkyPulse beta feedback:", message);
  }

  async function shareResult() {
    const skin = currentSkin();
    const message = `I scored ${flight.score} in SkyPulse with ${skin.name}. Can you beat ${progress.best}? ${location.origin}${location.pathname}`;
    try {
      if (navigator.share) {
        await navigator.share({ title: "SkyPulse flight", text: message });
        toast("FLIGHT SHARED");
        return;
      }
      if (navigator.clipboard?.writeText) {
        await navigator.clipboard.writeText(message);
        toast("FLIGHT LINK COPIED");
        return;
      }
    } catch (error) {
      if (error?.name === "AbortError") return;
    }
    window.prompt("Copy your SkyPulse flight:", message);
  }

  document.querySelector("#fly-button").addEventListener("click", startFlight);
  document.querySelector("#customize-button").addEventListener("click", () => { mode = "shop"; renderUi(); showShop(); });
  document.querySelector("#close-customize").addEventListener("click", goMenu);
  document.querySelector("#daily-button").addEventListener("click", () => { mode = "daily"; renderDaily(); saveProgress(); renderUi(); });
  document.querySelector("#close-daily").addEventListener("click", goMenu);
  document.querySelector("#daily-play").addEventListener("click", () => startFlight(true));
  document.querySelector("#retry-button").addEventListener("click", startFlight);
  document.querySelector("#results-menu").addEventListener("click", goMenu);
  document.querySelector("#pause-button").addEventListener("click", () => { mode = "paused"; renderUi(); });
  document.querySelector("#resume-button").addEventListener("click", () => { mode = "playing"; renderUi(); });
  document.querySelector("#restart-button").addEventListener("click", startFlight);
  document.querySelector("#pause-menu").addEventListener("click", goMenu);
  document.querySelector("#settings-button").addEventListener("click", () => { mode = "settings"; renderUi(); });
  document.querySelector("#close-settings").addEventListener("click", goMenu);
  ui.sound.addEventListener("click", () => { progress.sound = !progress.sound; saveProgress(); renderUi(); });
  ui.motion.addEventListener("click", () => { progress.reduceMotion = !progress.reduceMotion; saveProgress(); renderUi(); });
  ui.contrast.addEventListener("click", () => { progress.highContrast = !progress.highContrast; saveProgress(); renderUi(); });
  ui.feedback.addEventListener("click", shareFeedback);
  ui.resultShare.addEventListener("click", shareResult);
  ui.shopTabs.addEventListener("click", (event) => { if (event.target.dataset.category) showShop(event.target.dataset.category); });
  ui.menu.addEventListener("pointerdown", (event) => {
    if (event.target.closest?.("button, a")) return;
    startFlight();
  });
  canvas.addEventListener("pointerdown", (event) => {
    if (mode !== "playing" || event.isPrimary === false) return;
    event.preventDefault();
    flap();
  }, { passive: false });
  window.addEventListener("keydown", (event) => {
    if (event.code === "Space" || event.code === "ArrowUp") { event.preventDefault(); if (mode === "playing") flap(); else if (mode === "menu" || mode === "gameover") startFlight(); }
    if (event.code === "KeyP" && mode === "playing") { mode = "paused"; renderUi(); }
  });
  window.addEventListener("resize", resize);
  document.addEventListener("visibilitychange", () => { if (document.hidden && mode === "playing") { mode = "paused"; renderUi(); } });

  resize();
  resetFlight();
  renderUi();
  requestAnimationFrame(tick);

  if ("serviceWorker" in navigator) navigator.serviceWorker.register("./sw.js").catch(() => {});
})();
