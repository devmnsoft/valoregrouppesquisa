(function(){ window.UsageApi={ normalize:r=>r&&r.data?r.data:r,usage:()=>AjaxClient.get('/organization/current/usage'),limits:()=>AjaxClient.get('/organization/current/limits') }; }());
