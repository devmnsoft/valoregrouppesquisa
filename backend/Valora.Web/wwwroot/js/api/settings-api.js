(function(){ window.SettingsApi={ normalize:r=>r&&r.data?r.data:r,get:()=>AjaxClient.get('/settings'),update:d=>AjaxClient.put('/settings',d),me:()=>AjaxClient.get('/me') }; }());
