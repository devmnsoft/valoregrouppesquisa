(function(){ window.SettingsApi={ normalize:r=>r&&r.data?r.data:r,get:()=>AjaxClient.get('/bff/settings'),update:d=>AjaxClient.put('/bff/settings',d),me:()=>AjaxClient.get('/bff/me') }; }());
