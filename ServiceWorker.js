const cacheName = "Habis-RPG-v0.2.0";
const contentToCache = [
    "index.html",
    "Build/WebBuild.loader.js",
    "Build/WebBuild.framework.js",
    "Build/WebBuild.data",
    "Build/WebBuild.wasm",
    "TemplateData/style.css",
    "TemplateData/webmemd-icon.png",
    "manifest.webmanifest"
];

self.addEventListener('install', function (e) {
    console.log('[Service Worker] Install');
    e.waitUntil(
        caches.open(cacheName).then(function (cache) {
            console.log('[Service Worker] Caching app shell');
            return cache.addAll(contentToCache);
        })
    );
    self.skipWaiting();
});

self.addEventListener('activate', function (e) {
    console.log('[Service Worker] Activate');
    e.waitUntil(
        caches.keys().then(function (keyList) {
            return Promise.all(keyList.map(function (key) {
                if (key !== cacheName) {
                    console.log('[Service Worker] Removing old cache:', key);
                    return caches.delete(key);
                }
            }));
        })
    );
    self.clients.claim();
});

self.addEventListener('fetch', function (e) {
    e.respondWith(
        caches.match(e.request).then(function (response) {
            if (response) {
                return response;
            }
            return fetch(e.request).then(function (networkResponse) {
                if (networkResponse && networkResponse.status === 200 && networkResponse.type === 'basic') {
                    var responseToCache = networkResponse.clone();
                    caches.open(cacheName).then(function (cache) {
                        cache.put(e.request, responseToCache);
                    });
                }
                return networkResponse;
            });
        })
    );
});
