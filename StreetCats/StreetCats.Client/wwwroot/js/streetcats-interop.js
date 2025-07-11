/**
 * 🐱 STREETCATS JavaScript Interop
 * Funzioni per integrazione Blazor WebAssembly con browser APIs
 */

window.StreetCatsInterop = {
    // 🗺️ MAPPA LEAFLET
    maps: new Map(),
    markers: new Map(),

    /**
     * Inizializza una mappa Leaflet
     */
    initializeMap: function (elementId, latitude, longitude, zoom = 13) {
        try {
            // Rimuovi mappa esistente se presente
            if (this.maps.has(elementId)) {
                this.maps.get(elementId).remove();
            }

            // Crea nuova mappa
            const map = L.map(elementId).setView([latitude, longitude], zoom);

            // Aggiungi layer OpenStreetMap
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
                maxZoom: 19
            }).addTo(map);

            // Salva riferimento
            this.maps.set(elementId, map);
            this.markers.set(elementId, []);

            console.log(`🗺️ Mappa ${elementId} inizializzata:`, { latitude, longitude, zoom });
            return true;
        } catch (error) {
            console.error('❌ Errore inizializzazione mappa:', error);
            return false;
        }
    },

    /**
     * Aggiunge un marker alla mappa
     */
    addMarker: function (elementId, latitude, longitude, title, catId, popupContent = null) {
        try {
            const map = this.maps.get(elementId);
            if (!map) {
                console.error(`❌ Mappa ${elementId} non trovata`);
                return false;
            }

            // Crea marker personalizzato per gatto
            const catIcon = L.divIcon({
                html: '🐱',
                iconSize: [30, 30],
                className: 'cat-marker',
                iconAnchor: [15, 15]
            });

            const marker = L.marker([latitude, longitude], { icon: catIcon })
                .addTo(map);

            // Aggiungi popup se fornito
            if (popupContent) {
                marker.bindPopup(popupContent);
            } else if (title) {
                marker.bindPopup(`<b>${title}</b><br/>Clicca per dettagli`);
            }

            // Salva marker con ID gatto
            const markers = this.markers.get(elementId);
            markers.push({ marker, catId, title });

            console.log(`🐱 Marker aggiunto:`, { elementId, catId, title, latitude, longitude });
            return true;
        } catch (error) {
            console.error('❌ Errore aggiunta marker:', error);
            return false;
        }
    },

    /**
     * Rimuove tutti i marker dalla mappa
     */
    clearMarkers: function (elementId) {
        try {
            const map = this.maps.get(elementId);
            const markers = this.markers.get(elementId);

            if (map && markers) {
                markers.forEach(item => {
                    map.removeLayer(item.marker);
                });
                this.markers.set(elementId, []);
                console.log(`🧹 Marker rimossi da ${elementId}`);
                return true;
            }
            return false;
        } catch (error) {
            console.error('❌ Errore rimozione marker:', error);
            return false;
        }
    },

    /**
     * Centra la mappa su coordinate specifiche
     */
    setMapView: function (elementId, latitude, longitude, zoom = null) {
        try {
            const map = this.maps.get(elementId);
            if (map) {
                if (zoom !== null) {
                    map.setView([latitude, longitude], zoom);
                } else {
                    map.setView([latitude, longitude]);
                }
                console.log(`🎯 Mappa centrata:`, { elementId, latitude, longitude, zoom });
                return true;
            }
            return false;
        } catch (error) {
            console.error('❌ Errore centratura mappa:', error);
            return false;
        }
    },

    /**
     * Ottiene i bounds correnti della mappa
     */
    getMapBounds: function (elementId) {
        try {
            const map = this.maps.get(elementId);
            if (map) {
                const bounds = map.getBounds();
                return {
                    northEast: { lat: bounds.getNorthEast().lat, lng: bounds.getNorthEast().lng },
                    southWest: { lat: bounds.getSouthWest().lat, lng: bounds.getSouthWest().lng }
                };
            }
            return null;
        } catch (error) {
            console.error('❌ Errore recupero bounds:', error);
            return null;
        }
    },

    // 📍 GEOLOCALIZZAZIONE
    /**
     * Ottiene la posizione corrente dell'utente
     */
    getCurrentLocation: function () {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject('Geolocalizzazione non supportata dal browser');
                return;
            }

            const options = {
                enableHighAccuracy: true,
                timeout: 10000,
                maximumAge: 300000 // 5 minuti cache
            };

            navigator.geolocation.getCurrentPosition(
                (position) => {
                    const result = {
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude,
                        accuracy: position.coords.accuracy,
                        timestamp: new Date().toISOString()
                    };
                    console.log('📍 Posizione ottenuta:', result);
                    resolve(result);
                },
                (error) => {
                    let message = 'Errore geolocalizzazione';
                    switch (error.code) {
                        case error.PERMISSION_DENIED:
                            message = 'Permesso geolocalizzazione negato';
                            break;
                        case error.POSITION_UNAVAILABLE:
                            message = 'Posizione non disponibile';
                            break;
                        case error.TIMEOUT:
                            message = 'Timeout geolocalizzazione';
                            break;
                    }
                    console.error('❌', message, error);
                    reject(message);
                },
                options
            );
        });
    },

    // 💾 LOCAL STORAGE
    /**
     * Salva dati nel localStorage con gestione errori
     */
    setLocalStorage: function (key, value) {
        try {
            const serialized = JSON.stringify(value);
            localStorage.setItem(key, serialized);
            console.log(`💾 Salvato localStorage:`, { key, value });
            return true;
        } catch (error) {
            console.error('❌ Errore salvataggio localStorage:', error);
            return false;
        }
    },

    /**
     * Recupera dati dal localStorage
     */
    getLocalStorage: function (key) {
        try {
            const item = localStorage.getItem(key);
            if (item === null) return null;

            const parsed = JSON.parse(item);
            console.log(`📂 Recuperato localStorage:`, { key, value: parsed });
            return parsed;
        } catch (error) {
            console.error('❌ Errore recupero localStorage:', error);
            return null;
        }
    },

    /**
     * Rimuove dato dal localStorage
     */
    removeLocalStorage: function (key) {
        try {
            localStorage.removeItem(key);
            console.log(`🗑️ Rimosso localStorage:`, key);
            return true;
        } catch (error) {
            console.error('❌ Errore rimozione localStorage:', error);
            return false;
        }
    },

    // 📱 DEVICE & BROWSER
    /**
     * Verifica se il dispositivo è mobile
     */
    isMobileDevice: function () {
        return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
    },

    /**
     * Ottiene informazioni sul browser
     */
    getBrowserInfo: function () {
        return {
            userAgent: navigator.userAgent,
            language: navigator.language,
            cookieEnabled: navigator.cookieEnabled,
            onLine: navigator.onLine,
            platform: navigator.platform,
            isMobile: this.isMobileDevice()
        };
    },

    // 🔔 NOTIFICHE
    /**
     * Mostra notifica browser (se permesso)
     */
    showNotification: function (title, message, icon = '🐱') {
        if ('Notification' in window && Notification.permission === 'granted') {
            new Notification(title, {
                body: message,
                icon: icon,
                badge: icon,
                tag: 'streetcats'
            });
            return true;
        }
        return false;
    },

    /**
     * Richiede permesso notifiche
     */
    requestNotificationPermission: function () {
        if ('Notification' in window) {
            return Notification.requestPermission();
        }
        return Promise.reject('Notifiche non supportate');
    },

    // 🎨 UI UTILITIES
    /**
     * Scroll smooth verso elemento
     */
    scrollToElement: function (elementId, behavior = 'smooth') {
        const element = document.getElementById(elementId);
        if (element) {
            element.scrollIntoView({
                behavior: behavior,
                block: 'start',
                inline: 'nearest'
            });
            return true;
        }
        return false;
    },

    /**
     * Copia testo negli appunti
     */
    copyToClipboard: function (text) {
        if (navigator.clipboard) {
            return navigator.clipboard.writeText(text).then(() => {
                console.log('📋 Testo copiato:', text);
                return true;
            }).catch(error => {
                console.error('❌ Errore copia:', error);
                return false;
            });
        } else {
            // Fallback per browser più vecchi
            const textArea = document.createElement('textarea');
            textArea.value = text;
            document.body.appendChild(textArea);
            textArea.select();
            const success = document.execCommand('copy');
            document.body.removeChild(textArea);
            return Promise.resolve(success);
        }
    },

    // 🎯 EVENTI MAPPA INTERATTIVI
    /**
     * Configura callback per click sulla mappa
     */
    onMapClick: function (elementId, dotNetReference, methodName) {
        try {
            const map = this.maps.get(elementId);
            if (map) {
                map.on('click', function (e) {
                    const data = {
                        latitude: e.latlng.lat,
                        longitude: e.latlng.lng
                    };
                    console.log('🖱️ Click mappa:', data);
                    dotNetReference.invokeMethodAsync(methodName, data);
                });
                return true;
            }
            return false;
        } catch (error) {
            console.error('❌ Errore configurazione click mappa:', error);
            return false;
        }
    },

    // 🐱 MARKER INTERATTIVI CON CALLBACK

    /**
     * Variabile globale per il callback dei marker
     */
    markerClickCallback: null,

    /**
     * Imposta il callback per il click sui marker (chiamato da Map.razor)
     */
    setSelectCatCallback: function (dotNetReference) {
        try {
            this.markerClickCallback = dotNetReference;
            console.log('🎯 Callback marker click configurato');
            return true;
        } catch (error) {
            console.error('❌ Errore configurazione callback marker:', error);
            return false;
        }
    },

    /**
     * Aggiunge un marker con callback per il click
     */
    addMarkerWithCallback: function (elementId, latitude, longitude, title, catId, popupContent = null) {
        try {
            const map = this.maps.get(elementId);
            if (!map) {
                console.error(`❌ Mappa ${elementId} non trovata`);
                return false;
            }

            // Crea marker personalizzato per gatto
            const catIcon = L.divIcon({
                html: '🐱',
                iconSize: [30, 30],
                className: 'cat-marker',
                iconAnchor: [15, 15]
            });

            const marker = L.marker([latitude, longitude], { icon: catIcon })
                .addTo(map);

            // Aggiungi popup se fornito
            if (popupContent) {
                marker.bindPopup(popupContent);
            } else if (title) {
                marker.bindPopup(`<b>${title}</b><br/>Clicca per dettagli`);
            }

            // Aggiungi evento click sul marker
            const self = this;
            marker.on('click', function (e) {
                console.log('🐱 Click marker:', { catId, title });

                // Chiama il callback C# se configurato
                if (self.markerClickCallback) {
                    self.markerClickCallback.invokeMethodAsync('SelectCat', catId);
                }

                // Impedisci che il click si propaghi alla mappa
                L.DomEvent.stopPropagation(e);
            });

            // Salva marker con ID gatto
            const markers = this.markers.get(elementId);
            markers.push({ marker, catId, title });

            console.log(`🐱 Marker con callback aggiunto:`, { elementId, catId, title, latitude, longitude });
            return true;
        } catch (error) {
            console.error('❌ Errore aggiunta marker con callback:', error);
            return false;
        }
    }
};

// 🚀 INIZIALIZZAZIONE
console.log('🐱 STREETCATS JavaScript Interop caricato');

// Aggiungi stili CSS per marker personalizzati
const style = document.createElement('style');
style.textContent = `
    .cat-marker {
        background: white;
        border: 2px solid #f97316;
        border-radius: 50%;
        box-shadow: 0 2px 8px rgba(0,0,0,0.3);
        display: flex !important;
        align-items: center;
        justify-content: center;
        font-size: 18px;
        cursor: pointer;
        transition: transform 0.2s ease;
    }
    .cat-marker:hover {
        transform: scale(1.1);
        z-index: 1000;
    }
    .leaflet-popup-content {
        font-family: system-ui, sans-serif;
        font-size: 14px;
    }
`;
document.head.appendChild(style);