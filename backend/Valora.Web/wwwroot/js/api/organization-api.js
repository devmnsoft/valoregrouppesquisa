(function(){ window.OrganizationApi={ normalize:r=>r&&r.data?r.data:r,current:()=>AjaxClient.get('/organization/current'),update:d=>AjaxClient.put('/organization/current',d) }; }());
