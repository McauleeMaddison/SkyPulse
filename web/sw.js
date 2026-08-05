const CACHE = "skypulse-web-v12";
const CORE = [
  "./", "./index.html", "./privacy.html", "./styles.css?v=0.4.2", "./game.js?v=0.4.2", "./manifest.webmanifest",
  "../assets/images/branding/skypulse-app-icon.png",
  "../assets/images/backgrounds/neon-city.png",
  "../assets/images/characters/nova.png", "../assets/images/characters/nova-flap.png",
  "../assets/audio/flap.wav", "../assets/audio/score.wav", "../assets/audio/crash.wav",
];

self.addEventListener("install", (event) => {
  event.waitUntil(caches.open(CACHE).then((cache) => cache.addAll(CORE)));
  self.skipWaiting();
});

self.addEventListener("activate", (event) => event.waitUntil(
  caches.keys().then((keys) => Promise.all(keys.filter((key) => key !== CACHE).map((key) => caches.delete(key)))).then(() => self.clients.claim())
));

self.addEventListener("fetch", (event) => {
  if (event.request.method !== "GET") return;
  const url = new URL(event.request.url);
  const appShell = url.origin === self.location.origin && (
    event.request.mode === "navigate" ||
    ["/web/index.html", "/web/styles.css", "/web/game.js", "/web/manifest.webmanifest"].some((path) => url.pathname.endsWith(path))
  );
  const cacheResponse = (response) => {
    if (response.ok && new URL(event.request.url).origin === self.location.origin) {
      const copy = response.clone();
      caches.open(CACHE).then((cache) => cache.put(event.request, copy));
    }
    return response;
  };
  if (appShell) {
    event.respondWith(fetch(event.request).then(cacheResponse).catch(() => caches.match(event.request).then((cached) => cached || caches.match("./"))));
    return;
  }
  event.respondWith(caches.match(event.request).then((cached) => cached || fetch(event.request).then(cacheResponse)));
});
