const {ok,css}=require('./_legacy-final-validators');
ok(/overflow-x:hidden/.test(css),'overflow-x hidden configured');
ok(/font-size:clamp\(42px,12vw,88px\)/.test(css.replace(/\s+/g,'')),'score font clamps on mobile');
ok(/@media \(max-width:640px\)/.test(css)&&/grid-template-columns:1fr/.test(css),'mobile one-column actions');
