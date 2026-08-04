(() => {
  "use strict";

  const canvas = document.querySelector("#game");
  const ctx = canvas.getContext("2d", { alpha: false });
  const ui = {
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
    shopGrid: document.querySelector("#shop-grid"),
    shopTabs: document.querySelector("#shop-tabs"),
    gameOver: document.querySelector("#game-over"),
    resultScore: document.querySelector("#result-score"),
    resultBest: document.querySelector("#result-best"),
    resultCrystals: document.querySelector("#result-crystals"),
    pause: document.querySelector("#pause"),
    settings: document.querySelector("#settings"),
    sound: document.querySelector("#sound-toggle"),
    motion: document.querySelector("#motion-toggle"),
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
    { id: "neon_city", name: "NEON CITY", price: 0, accent: "#45eaff", background: "images/backgrounds/neon-city.png", floor: "#0a0522", pipe: "#45eaff" },
    { id: "aurora_rise", name: "AURORA RISE", price: 55, accent: "#61f5b3", background: "images/backgrounds/neon-city.png", floor: "#05251e", pipe: "#61f5b3" },
    { id: "solar_drift", name: "SOLAR DRIFT", price: 90, accent: "#ffc34d", background: "images/backgrounds/neon-city.png", floor: "#2b0d10", pipe: "#ffc34d" },
    { id: "midnight_tide", name: "MIDNIGHT TIDE", price: 125, accent: "#45eaff", background: "images/backgrounds/neon-city.png", floor: "#07113d", pipe: "#45eaff" },
    { id: "velvet_dawn", name: "VELVET DAWN", price: 160, accent: "#f05bc6", background: "images/backgrounds/neon-city.png", floor: "#26051f", pipe: "#f05bc6" },
    { id: "crystal_night", name: "CRYSTAL NIGHT", price: 190, accent: "#edf7ff", background: "images/backgrounds/themes/crystal-night.png", floor: "#071239", pipe: "#edf7ff" },
    { id: "jade_horizon", name: "JADE HORIZON", price: 215, accent: "#61f5b3", background: "images/backgrounds/themes/jade-horizon.png", floor: "#063523", pipe: "#61f5b3" },
    { id: "rose_orbit", name: "ROSE ORBIT", price: 240, accent: "#f05bc6", background: "images/backgrounds/themes/rose-orbit.png", floor: "#3d0925", pipe: "#f05bc6" },
    { id: "cobalt_storm", name: "COBALT STORM", price: 265, accent: "#45eaff", background: "images/backgrounds/themes/cobalt-storm.png", floor: "#06153c", pipe: "#45eaff" },
    { id: "amber_skies", name: "AMBER SKIES", price: 290, accent: "#ffc34d", background: "images/backgrounds/themes/amber-skies.png", floor: "#3b1208", pipe: "#ffc34d" },
    { id: "violet_rain", name: "VIOLET RAIN", price: 315, accent: "#b17cff", background: "images/backgrounds/themes/violet-rain.png", floor: "#210842", pipe: "#b17cff" },
    { id: "polar_glow", name: "POLAR GLOW", price: 340, accent: "#edf7ff", background: "images/backgrounds/themes/polar-glow.png", floor: "#073144", pipe: "#edf7ff" },
    { id: "eclipse", name: "ECLIPSE", price: 365, accent: "#b17cff", background: "images/backgrounds/themes/eclipse.png", floor: "#10051f", pipe: "#b17cff" },
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

  const fallbackProgress = {
    crystals: 0,
    best: 0,
    sound: true,
    reduceMotion: false,
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
  const audioCache = new Map();
  const audioPaths = {
    flap: "audio/flap.wav", score: "audio/score.wav", crash: "audio/crash.wav", crystal: "audio/crystal.wav", best: "audio/new-best.wav",
  };
  let activeCategory = "skins";
  let mode = "menu";
  let lastFrame = performance.now();
  let toastTimer = 0;
  let width = 420;
  let height = 860;

  const flight = {
    bird: { x: 0, y: 0, velocity: 0, tilt: 0, wing: 0 },
    pipes: [],
    crests: [],
    trail: [],
    bursts: [],
    score: 0,
    earned: 0,
    spawnTimer: 1.15,
    runningTime: 0,
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

  function playSound(name) {
    if (!progress.sound || !audioPaths[name]) return;
    try {
      if (!audioCache.has(name)) audioCache.set(name, new Audio(ASSET + audioPaths[name]));
      const sound = audioCache.get(name).cloneNode();
      sound.volume = name === "crash" ? .45 : .32;
      sound.play().catch(() => {});
    } catch { /* audio is optional */ }
  }

  function currentSkin() { return skinById[progress.equipped.skin]; }
  function currentTheme() { return themeById[progress.equipped.theme]; }
  function currentTrail() { return trailById[progress.equipped.trail]; }
  function currentPipe() { return pipeById[progress.equipped.pipe]; }

  function resize() {
    const rect = canvas.getBoundingClientRect();
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    width = Math.max(1, rect.width);
    height = Math.max(1, rect.height);
    canvas.width = Math.round(width * dpr);
    canvas.height = Math.round(height * dpr);
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    if (mode !== "playing") resetFlight();
  }

  function setVisible(element, visible) { element.classList.toggle("hidden", !visible); }

  function renderUi() {
    const skin = currentSkin();
    ui.crystals.textContent = `✦ ${progress.crystals}`;
    ui.menuCurrency.textContent = `✦ ${progress.crystals}`;
    ui.shopCurrency.textContent = `✦ ${progress.crystals}`;
    ui.best.textContent = `BEST · ${progress.best}`;
    ui.menuBird.src = ASSET + skin.art;
    ui.equipped.textContent = `${skin.name} EQUIPPED`;
    ui.sound.textContent = `SOUND  ${progress.sound ? "ON" : "OFF"}`;
    ui.motion.textContent = `REDUCED MOTION  ${progress.reduceMotion ? "ON" : "OFF"}`;
    setVisible(ui.menu, mode === "menu");
    setVisible(ui.hud, mode === "playing");
    setVisible(ui.customize, mode === "shop");
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

  function resetFlight() {
    const bird = flight.bird;
    bird.x = width * .31;
    bird.y = height * .52;
    bird.velocity = 0;
    bird.tilt = 0;
    bird.wing = 0;
    flight.pipes = [];
    flight.crests = [];
    flight.trail = [];
    flight.bursts = [];
    flight.score = 0;
    flight.earned = 0;
    flight.spawnTimer = .82;
    flight.runningTime = 0;
  }

  function startFlight() {
    resetFlight();
    mode = "playing";
    flap();
    renderUi();
  }

  function flap() {
    if (mode !== "playing") return;
    const bird = flight.bird;
    bird.velocity = Math.min(bird.velocity < 0 ? -430 : bird.velocity - 340, -395);
    bird.wing = .34;
    playSound("flap");
    if (navigator.vibrate) navigator.vibrate(8);
  }

  function addPipe() {
    const ground = height * .88;
    const gap = Math.max(height * .205, height * .284 - Math.min(flight.score, 25) * 1.4);
    const min = height * .17 + gap / 2;
    const max = ground - gap / 2 - height * .05;
    const gapY = min + Math.random() * Math.max(1, max - min);
    flight.pipes.push({ x: width + 35, gapY, gap, passed: false });
    if (Math.random() < .61) flight.crests.push({ x: width + 78, y: gapY + (Math.random() - .5) * gap * .30, phase: Math.random() * Math.PI * 2 });
  }

  function endFlight() {
    if (mode !== "playing") return;
    mode = "gameover";
    progress.best = Math.max(progress.best, flight.score);
    saveProgress();
    playSound("crash");
    if (navigator.vibrate) navigator.vibrate([16, 36, 26]);
    ui.resultScore.textContent = `SCORE  ${flight.score}`;
    ui.resultBest.textContent = flight.score >= progress.best && flight.score > 0 ? `NEW BEST  ${progress.best}` : `BEST  ${progress.best}`;
    ui.resultCrystals.textContent = `CRESTS  +${flight.earned}`;
    renderUi();
  }

  function update(dt) {
    if (mode !== "playing") return;
    const bird = flight.bird;
    const ground = height * .88;
    const speed = width * (.57 + Math.min(flight.score, 25) * .007);
    flight.runningTime += dt;
    bird.wing = Math.max(0, bird.wing - dt);
    bird.velocity = Math.min(730, bird.velocity + 1070 * dt);
    bird.y += bird.velocity * dt;
    const targetTilt = Math.max(-30, Math.min(24, bird.velocity * .072));
    bird.tilt += (targetTilt - bird.tilt) * Math.min(1, dt * (targetTilt > bird.tilt ? 12 : 5.5));
    flight.trail.unshift({ x: bird.x - 27, y: bird.y, age: 0 });
    flight.trail = flight.trail.slice(0, 22).map((point) => ({ ...point, age: point.age + dt })).filter((point) => point.age < .46);
    flight.spawnTimer -= dt;
    if (flight.spawnTimer <= 0) {
      addPipe();
      flight.spawnTimer = 1.48;
    }
    const pipeWidth = Math.max(62, width * .16);
    const bodyHalfW = 18;
    const bodyHalfH = 14;
    for (const pipe of flight.pipes) {
      pipe.x -= speed * dt;
      if (!pipe.passed && pipe.x + pipeWidth < bird.x - bodyHalfW) {
        pipe.passed = true;
        flight.score += 1;
        flight.bursts.push({ x: bird.x + 24, y: bird.y + 23, age: 0, colour: currentSkin().accent });
        playSound("score");
      }
      const overlapsX = bird.x + bodyHalfW > pipe.x && bird.x - bodyHalfW < pipe.x + pipeWidth;
      if (overlapsX && (bird.y - bodyHalfH < pipe.gapY - pipe.gap / 2 || bird.y + bodyHalfH > pipe.gapY + pipe.gap / 2)) endFlight();
    }
    flight.pipes = flight.pipes.filter((pipe) => pipe.x + pipeWidth > -50);
    for (const crest of flight.crests) {
      crest.x -= speed * dt;
      crest.phase += dt * 3;
      if (Math.hypot(crest.x - bird.x, crest.y - bird.y) < 32) {
        crest.x = -100;
        progress.crystals += 1;
        flight.earned += 1;
        saveProgress();
        playSound("crystal");
      }
    }
    flight.crests = flight.crests.filter((crest) => crest.x > -40);
    flight.bursts = flight.bursts.map((burst) => ({ ...burst, age: burst.age + dt })).filter((burst) => burst.age < .66);
    if (bird.y < -20 || bird.y + bodyHalfH >= ground) endFlight();
  }

  function cover(imageItem) {
    if (!imageItem?.complete || !imageItem.naturalWidth) return false;
    const scale = Math.max(width / imageItem.naturalWidth, height / imageItem.naturalHeight);
    const drawW = imageItem.naturalWidth * scale;
    const drawH = imageItem.naturalHeight * scale;
    ctx.drawImage(imageItem, (width - drawW) / 2, (height - drawH) / 2, drawW, drawH);
    return true;
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
    ctx.fillStyle = "#07031b";
    ctx.fillRect(0, 0, width, height);
    cover(image(theme.background));
    ctx.fillStyle = "rgba(7, 2, 28, .10)";
    ctx.fillRect(0, 0, width, height);
    if (mode === "playing" && !progress.reduceMotion) {
      ctx.fillStyle = `${theme.accent}16`;
      for (let index = 0; index < 13; index += 1) {
        const x = (index * 71 + flight.runningTime * (5 + index % 3)) % (width + 16) - 8;
        const y = height * (.20 + (index * .067) % .55);
        ctx.fillRect(x, y, index % 4 === 0 ? 2 : 1, index % 4 === 0 ? 2 : 1);
      }
    }
  }

  function drawFloor() {
    const theme = currentTheme();
    const floorY = height * .88;
    ctx.fillStyle = theme.floor;
    ctx.fillRect(0, floorY, width, height - floorY);
    ctx.fillStyle = `${theme.accent}36`;
    ctx.fillRect(0, floorY, width, 9);
    ctx.strokeStyle = `${theme.accent}bb`;
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(0, floorY + 1);
    ctx.lineTo(width, floorY + 1);
    ctx.stroke();
    ctx.strokeStyle = `${theme.accent}28`;
    ctx.lineWidth = 1;
    for (const endX of [width * .23, width * .77]) {
      ctx.beginPath();
      ctx.moveTo(width * .5 + (endX - width * .5) * .16, floorY + 7);
      ctx.lineTo(endX, height);
      ctx.stroke();
    }
  }

  function drawPipe(pipe) {
    const pipeStyle = currentPipe();
    const floorY = height * .88;
    const pipeWidth = Math.max(62, width * .16);
    const sections = [[0, pipe.gapY - pipe.gap / 2], [pipe.gapY + pipe.gap / 2, floorY - (pipe.gapY + pipe.gap / 2)]];
    for (const [y, h] of sections) {
      if (h <= 0) continue;
      ctx.fillStyle = "#070b23";
      roundedRect(pipe.x, y, pipeWidth, h, 7);
      ctx.fill();
      ctx.fillStyle = pipeStyle.panel;
      roundedRect(pipe.x + 8, y + 4, pipeWidth - 16, Math.max(0, h - 8), 4);
      ctx.fill();
      ctx.fillStyle = pipeStyle.energy;
      ctx.fillRect(pipe.x + pipeWidth / 2 - 1.5, y + 7, 3, Math.max(0, h - 14));
      ctx.strokeStyle = pipeStyle.accent;
      ctx.globalAlpha = .82;
      ctx.lineWidth = 1.4;
      ctx.strokeRect(pipe.x + .7, y + .7, pipeWidth - 1.4, h - 1.4);
      ctx.globalAlpha = 1;
    }
    for (const y of [pipe.gapY - pipe.gap / 2 - 11, pipe.gapY + pipe.gap / 2 - 4]) {
      ctx.fillStyle = pipeStyle.accent;
      roundedRect(pipe.x - 6, y, pipeWidth + 12, 14, 5);
      ctx.fill();
      ctx.strokeStyle = "rgba(255,255,255,.55)";
      ctx.lineWidth = 1;
      ctx.beginPath();
      ctx.moveTo(pipe.x - 3, y + 10);
      ctx.lineTo(pipe.x + pipeWidth + 3, y + 10);
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
    for (const [index, colour] of trail.colours.entries()) {
      ctx.save();
      ctx.strokeStyle = colour;
      ctx.globalAlpha = index ? .42 : .78;
      ctx.lineWidth = index ? 7 : 3;
      ctx.lineCap = "round";
      ctx.beginPath();
      points.forEach((point, pointIndex) => {
        const x = point.x - point.age * 132;
        const y = point.y + Math.sin(point.age * 16 + index * 2) * (index ? 3 : 1);
        if (pointIndex === 0) ctx.moveTo(x, y); else ctx.lineTo(x, y);
      });
      ctx.stroke();
      ctx.restore();
    }
  }

  function drawBird() {
    const bird = flight.bird;
    const skin = currentSkin();
    const base = image(skin.art);
    const flapFrame = image(skin.flap);
    const wingMix = mode === "playing" ? Math.max(0, Math.min(1, bird.wing / .34)) : .42 + Math.sin(performance.now() * .005) * .18;
    const artWidth = width * .30 * (skin.scale || 1);
    ctx.save();
    ctx.translate(bird.x, bird.y);
    ctx.rotate(bird.tilt * Math.PI / 180);
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
      ctx.globalCompositeOperation = "source-atop";
      ctx.fillStyle = tint;
      ctx.globalAlpha *= .22;
      ctx.fillRect(-artWidth * .60, -artHeight * .50, artWidth, artHeight);
      ctx.globalCompositeOperation = "source-over";
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

  function draw() {
    drawBackground();
    if (mode === "playing" || mode === "paused" || mode === "gameover") {
      for (const pipe of flight.pipes) drawPipe(pipe);
      for (const crest of flight.crests) drawCrest(crest);
      drawTrail();
      drawBird();
      drawBursts();
      drawFloor();
    }
    if (mode === "playing") ui.score.textContent = flight.score;
  }

  function tick(now) {
    const dt = Math.min(.034, Math.max(0, (now - lastFrame) / 1000));
    lastFrame = now;
    update(dt);
    draw();
    requestAnimationFrame(tick);
  }

  function showShop(category = activeCategory) {
    activeCategory = category;
    const items = catalog[category];
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
    if (category === "skins") preview = `<div class="shop-preview"><img src="${ASSET + item.art}" alt=""></div>`;
    if (category === "themes") preview = `<div class="shop-preview"><span class="theme-preview" style="background-image:url('${ASSET + item.background}')"></span></div>`;
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
    renderUi();
  }

  document.querySelector("#fly-button").addEventListener("click", startFlight);
  document.querySelector("#customize-button").addEventListener("click", () => { mode = "shop"; renderUi(); showShop(); });
  document.querySelector("#close-customize").addEventListener("click", goMenu);
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
  ui.shopTabs.addEventListener("click", (event) => { if (event.target.dataset.category) showShop(event.target.dataset.category); });
  ui.menu.addEventListener("pointerdown", (event) => { if (event.target === ui.menu) startFlight(); });
  canvas.addEventListener("pointerdown", (event) => { if (mode === "playing") { event.preventDefault(); flap(); } });
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
