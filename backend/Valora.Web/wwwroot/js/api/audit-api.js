(function(){ window.AuditApi={ normalize:r=>r&&r.data?r.data:r,events:q=>AjaxClient.get('/bff/audit'+(q?'?'+$.param(q):'')) }; }());
