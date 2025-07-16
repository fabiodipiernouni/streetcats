/**
 * StreetCats JavaScript Interop
 * Enhanced with Photo Upload Integration
 */

window.StreetCatsInterop = {
    // Configuration
    config: {
        mapInstances: new Map(),
        authToken: null,
        apiBaseUrl: 'http://localhost:3000/api'
    },

    // ========================================
    // PHOTO UPLOAD FUNCTIONS
    // ========================================

    /**
     * Initialize photo upload component
     */
    initializePhotoUpload: function (componentRef) {
        console.log('Initializing photo upload component');

        // Store component reference for callbacks
        this.config.photoUploadComponent = componentRef;

        // Setup paste event listener for Ctrl+V photo paste
        document.addEventListener('paste', (e) => this.handlePasteImage(e, componentRef));

        return true;
    },

    /**
     * Upload photo from Blazor component (base64 data)
     */
    uploadPhotoFromBlazor: async function (uploadUrl, uploadData, progressCallback) {
        try {
            console.log('Starting photo upload from Blazor to:', uploadUrl);

            // Convert base64 to blob
            const byteCharacters = atob(uploadData.data);
            const byteNumbers = new Array(byteCharacters.length);
            for (let i = 0; i < byteCharacters.length; i++) {
                byteNumbers[i] = byteCharacters.charCodeAt(i);
            }
            const byteArray = new Uint8Array(byteNumbers);
            const blob = new Blob([byteArray], { type: uploadData.contentType });

            // Create FormData
            const formData = new FormData();
            formData.append('photo', blob, uploadData.fileName);

            // Get auth token
            const token = this.getAuthToken();

            // Setup headers
            const headers = {};
            if (token) {
                headers['Authorization'] = `Bearer ${token}`;
            }

            // Upload with progress tracking
            const response = await this.uploadWithProgress(uploadUrl, formData, headers, progressCallback);

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || `HTTP ${response.status}`);
            }

            const result = await response.json();
            console.log('Photo upload from Blazor successful:', result);

            return JSON.stringify(result);

        } catch (error) {
            console.error('Photo upload from Blazor failed:', error);
            throw error;
        }
    },

    /**
     * Upload with XMLHttpRequest for progress tracking
     */
    uploadWithProgress: function (url, formData, headers, progressCallback) {
        return new Promise((resolve, reject) => {
            const xhr = new XMLHttpRequest();

            // Progress tracking
            if (progressCallback) {
                xhr.upload.addEventListener('progress', (e) => {
                    if (e.lengthComputable) {
                        const percentComplete = Math.round((e.loaded / e.total) * 100);
                        try {
                            progressCallback.invokeMethodAsync('UpdateUploadProgress', percentComplete);
                        } catch (err) {
                            console.warn('Progress callback failed:', err);
                        }
                    }
                });
            }

            // Setup request
            xhr.open('POST', url, true);

            // Add headers
            Object.entries(headers).forEach(([key, value]) => {
                xhr.setRequestHeader(key, value);
            });

            // Handle response
            xhr.onload = () => {
                if (xhr.status >= 200 && xhr.status < 300) {
                    resolve({
                        ok: true,
                        status: xhr.status,
                        json: () => Promise.resolve(JSON.parse(xhr.responseText))
                    });
                } else {
                    reject(new Error(`Upload failed: ${xhr.status} ${xhr.statusText}`));
                }
            };

            xhr.onerror = () => reject(new Error('Network error during upload'));
            xhr.ontimeout = () => reject(new Error('Upload timeout'));

            // Set timeout (30 seconds for large files)
            xhr.timeout = 30000;

            // Send request
            xhr.send(formData);
        });
    },

    /**
     * Validate image file before upload
     */
    validateImageFile: function (file) {
        const MAX_SIZE = 5 * 1024 * 1024; // 5MB
        const ALLOWED_TYPES = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];

        if (!file) {
            return { isValid: false, error: 'No file provided' };
        }

        if (!ALLOWED_TYPES.includes(file.type.toLowerCase())) {
            return { isValid: false, error: 'Invalid file type. Use JPG, PNG, or WebP.' };
        }

        if (file.size > MAX_SIZE) {
            return { isValid: false, error: 'File too large. Maximum 5MB allowed.' };
        }

        return { isValid: true };
    },

    /**
     * Handle paste events for image upload
     */
    handlePasteImage: function (event, componentRef) {
        const items = event.clipboardData?.items;
        if (!items) return;

        for (let item of items) {
            if (item.type.startsWith('image/')) {
                event.preventDefault();

                const file = item.getAsFile();
                if (file) {
                    console.log('Image pasted from clipboard:', file.name || 'clipboard-image');

                    // Convert to base64 and send to Blazor component
                    this.fileToBase64(file)
                        .then(base64 => {
                            componentRef.invokeMethodAsync('HandleDroppedFile',
                                file.name || 'pasted-image.png',
                                file.type,
                                file.size,
                                base64
                            );
                        })
                        .catch(err => {
                            console.error('Error processing pasted image:', err);
                        });
                }
                break;
            }
        }
    },

    /**
     * Handle file drop for drag & drop upload
     */
    handleFileDrop: function (componentRef) {
        // This is called after ondrop event in Blazor
        // File data is handled by browser's drag & drop API
        console.log('File drop handled by Blazor component');
        return true;
    },

    /**
     * Trigger file input dialog
     */
    triggerFileInput: function (inputElement) {
        if (inputElement && inputElement.click) {
            inputElement.click();
        }
    },

    /**
     * Convert file to base64 string
     */
    fileToBase64: function (file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => {
                const base64 = reader.result.split(',')[1]; // Remove data:image/...;base64, prefix
                resolve(base64);
            };
            reader.onerror = reject;
            reader.readAsDataURL(file);
        });
    },

    // ========================================
    // AUTHENTICATION HELPERS
    // ========================================

    /**
     * Get stored auth token
     */
    getAuthToken: function () {
        try {
            return localStorage.getItem('streetcats_token') || null;
        } catch (error) {
            console.warn('Cannot access localStorage for auth token');
            return null;
        }
    },

    /**
     * Set auth token for API calls
     */
    setAuthToken: function (token) {
        try {
            if (token) {
                localStorage.setItem('streetcats_token', token);
                this.config.authToken = token;
            } else {
                localStorage.removeItem('streetcats_token');
                this.config.authToken = null;
            }
            console.log('Auth token updated');
        } catch (error) {
            console.warn('Cannot store auth token in localStorage');
        }
    },

    // ========================================
    // MAP FUNCTIONS (Existing)
    // ========================================

    /**
     * Initialize Leaflet map
     */
    initializeMap: function (mapId, latitude, longitude, zoom = 13) {
        try {
            // Check if Leaflet is loaded
            if (typeof L === 'undefined') {
                console.error('Leaflet library not loaded');
                return false;
            }

            // Destroy existing map
            if (this.config.mapInstances.has(mapId)) {
                this.config.mapInstances.get(mapId).remove();
            }

            // Create new map
            const map = L.map(mapId).setView([latitude, longitude], zoom);

            // Add OpenStreetMap tiles
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '© OpenStreetMap contributors',
                maxZoom: 19
            }).addTo(map);

            // Store map instance
            this.config.mapInstances.set(mapId, map);

            console.log(`Map initialized: ${mapId} at (${latitude}, ${longitude})`);
            return true;

        } catch (error) {
            console.error('Map initialization failed:', error);
            return false;
        }
    },

    /**
     * Add marker to map
     */
    addMapMarker: function (mapId, latitude, longitude, title, popupContent, iconColor = 'blue') {
        try {
            const map = this.config.mapInstances.get(mapId);
            if (!map) {
                console.error(`Map not found: ${mapId}`);
                return false;
            }

            // Custom marker icon based on cat status
            const icon = this.createCatIcon(iconColor);

            // Create marker
            const marker = L.marker([latitude, longitude], { icon })
                .addTo(map);

            // Add popup if content provided
            if (popupContent) {
                marker.bindPopup(popupContent);
            }

            // Add title
            if (title) {
                marker.bindTooltip(title);
            }

            console.log(`Marker added to ${mapId}: ${title}`);
            return true;

        } catch (error) {
            console.error('Add marker failed:', error);
            return false;
        }
    },

    /**
     * Create custom cat marker icon
     */
    createCatIcon: function (color = 'blue') {
        const colors = {
            'red': '#ef4444',     // missing
            'green': '#10b981',   // found
            'blue': '#3b82f6',    // seen
            'yellow': '#f59e0b',  // injured
            'purple': '#8b5cf6'   // adopted
        };

        const iconColor = colors[color] || colors.blue;

        return L.divIcon({
            className: 'custom-cat-marker',
            html: `
                <div style="
                    background: ${iconColor};
                    width: 30px;
                    height: 30px;
                    border-radius: 50%;
                    border: 3px solid white;
                    box-shadow: 0 2px 4px rgba(0,0,0,0.3);
                    display: flex;
                    align-items: center;
                    justify-content: center;
                ">
                    <span style="color: white; font-size: 14px;">CAT</span>
                </div>
            `,
            iconSize: [30, 30],
            iconAnchor: [15, 15],
            popupAnchor: [0, -15]
        });
    },

    /**
     * Clear all markers from map
     */
    clearMapMarkers: function (mapId) {
        try {
            const map = this.config.mapInstances.get(mapId);
            if (!map) return false;

            map.eachLayer(layer => {
                if (layer instanceof L.Marker) {
                    map.removeLayer(layer);
                }
            });

            console.log(`Markers cleared from ${mapId}`);
            return true;

        } catch (error) {
            console.error('Clear markers failed:', error);
            return false;
        }
    },

    // ========================================
    // GEOLOCATION FUNCTIONS
    // ========================================

    /**
     * Get current location using browser geolocation
     */
    getCurrentLocation: function () {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject(new Error('Geolocation not supported'));
                return;
            }

            const options = {
                enableHighAccuracy: true,
                timeout: 10000,
                maximumAge: 60000 // Cache for 1 minute
            };

            navigator.geolocation.getCurrentPosition(
                position => {
                    const result = {
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude,
                        accuracy: position.coords.accuracy,
                        timestamp: new Date().toISOString()
                    };

                    console.log('Location obtained:', result);
                    resolve(result);
                },
                error => {
                    console.error('Geolocation error:', error);

                    // Provide fallback location (Naples center)
                    const fallback = {
                        latitude: 40.8518,
                        longitude: 14.2681,
                        accuracy: null,
                        timestamp: new Date().toISOString()
                    };

                    console.log('Using fallback location (Naples)');
                    resolve(fallback);
                },
                options
            );
        });
    },

    // ========================================
    // UTILITY FUNCTIONS
    // ========================================

    /**
     * Show toast notification
     */
    showToast: function (message, type = 'info', duration = 3000) {
        // Create toast element
        const toast = document.createElement('div');
        toast.className = `streetcats-toast toast-${type}`;
        toast.textContent = message;

        // Styling
        Object.assign(toast.style, {
            position: 'fixed',
            top: '20px',
            right: '20px',
            background: type === 'error' ? '#ef4444' : type === 'success' ? '#10b981' : '#3b82f6',
            color: 'white',
            padding: '12px 16px',
            borderRadius: '8px',
            boxShadow: '0 4px 6px rgba(0, 0, 0, 0.1)',
            zIndex: '10000',
            fontSize: '14px',
            fontWeight: '500',
            opacity: '0',
            transition: 'opacity 0.3s ease',
            maxWidth: '300px'
        });

        // Add to page
        document.body.appendChild(toast);

        // Animate in
        requestAnimationFrame(() => {
            toast.style.opacity = '1';
        });

        // Remove after duration
        setTimeout(() => {
            toast.style.opacity = '0';
            setTimeout(() => {
                if (toast.parentNode) {
                    toast.parentNode.removeChild(toast);
                }
            }, 300);
        }, duration);
    },

    /**
     * Log debug info
     */
    logDebug: function (message, data = null) {
        console.log(`StreetCats: ${message}`, data || '');
    },

    /**
     * Initialize all StreetCats components
     */
    initialize: function () {
        console.log('StreetCats JavaScript Interop initialized');

        // Check for required libraries
        const checks = {
            leaflet: typeof L !== 'undefined',
            geolocation: 'geolocation' in navigator,
            localStorage: this.testLocalStorage(),
            fileApi: 'File' in window && 'FileReader' in window
        };

        console.log('Environment checks:', checks);

        return checks;
    },

    /**
     * Test localStorage availability
     */
    testLocalStorage: function () {
        try {
            const test = 'streetcats_test';
            localStorage.setItem(test, 'test');
            localStorage.removeItem(test);
            return true;
        } catch (e) {
            return false;
        }
    }
};

// Auto-initialize when script loads
document.addEventListener('DOMContentLoaded', () => {
    window.StreetCatsInterop.initialize();
});

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = window.StreetCatsInterop;
}