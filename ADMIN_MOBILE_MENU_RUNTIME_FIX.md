# Correção runtime do menu mobile admin

`app.js` centraliza os itens em `getAdminMenuItems(user)`, renderiza `renderAdminSidebar(user,currentRoute)` e vincula overlay, ESC, resize e fechamento após clique. `style.css` remove cortes de altura/overflow e fixa a sidebar com rolagem vertical.
