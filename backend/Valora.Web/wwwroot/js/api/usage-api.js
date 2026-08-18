(function(){ window.UsageApi={ normalize:r=>r&&r.data?r.data:r,usage:()=>AjaxClient.get('/bff/organization/current/usage'),limits:()=>AjaxClient.get('/bff/plans/limits') }; }());
