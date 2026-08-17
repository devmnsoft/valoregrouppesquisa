window.PlansApi={
  public:()=>AjaxClient.get('/plans/public'),
  current:()=>AjaxClient.get('/bff/plans/current'),
  usage:()=>AjaxClient.get('/bff/plans/usage'),
  features:()=>AjaxClient.get('/bff/plans/features'),
  limits:()=>AjaxClient.get('/bff/plans/limits')
};
