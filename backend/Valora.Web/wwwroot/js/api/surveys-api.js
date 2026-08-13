(function(){
  const base='/bff/surveys';
  window.SurveysApi={
    normalize:r=>r&&r.data?r.data:r,
    list:()=>AjaxClient.get(base),
    get:id=>AjaxClient.get(base+'/'+encodeURIComponent(id)),
    create:d=>AjaxClient.post(base,d),
    update:(id,d)=>AjaxClient.put(base+'/'+encodeURIComponent(id),d),
    setStatus:(id,s)=>AjaxClient.patch(base+'/'+encodeURIComponent(id)+'/status',{status:s}),
    links:id=>AjaxClient.get(base+'/'+encodeURIComponent(id)+'/links'),
    createLink:id=>AjaxClient.post(base+'/'+encodeURIComponent(id)+'/links',{}),
    revokeLink:id=>AjaxClient.patch('/bff/survey-links/'+encodeURIComponent(id)+'/status',{status:'revoked'})
  };
}());
