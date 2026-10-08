(function(){ window.AuditApi={ normalize:r=>r&&r.data?r.data:r,events:q=>{ const s=q&&Object.keys(q).length?'?'+new URLSearchParams(q).toString():''; return AjaxClient.get('/bff/audit'+s); } }; }());
