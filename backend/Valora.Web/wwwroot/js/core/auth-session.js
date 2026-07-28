// Authentication state is owned by the server-side BFF cookie; JavaScript never receives tokens.
window.Session={save:()=>{},token:()=>null,clear:()=>{},isAuthenticated:()=>document.body.dataset.authenticated==='true'};
