const CACHE = "skypulse-web-v8";
const CORE = [
  "./", "./index.html", "./privacy.html", "./styles.css", "./game.js", "./manifest.webmanifest",
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
  event.respondWith(caches.match(event.request).then((cached) => cached || fetch(event.request).then((response) => {
    if (response.ok && new URL(event.request.url).origin === self.location.origin) {
      const copy = response.clone();
      caches.open(CACHE).then((cache) => cache.put(event.request, copy));
    }
    return response;
  })));
});
