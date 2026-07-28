# Armazenamento distribuído de sessão BFF

O navegador mantém apenas o cookie HttpOnly `__Host-Valora.Session`, cujo principal inclui um ticket aleatório. Access e refresh tokens vivem no payload servidor protegido por ASP.NET Core Data Protection e armazenado via `IDistributedCache`.

Desenvolvimento usa `AddDistributedMemoryCache`. Homologação e produção devem substituir o provider pela implementação Redis compartilhada e persistir o key ring de Data Protection entre instâncias. A entrada possui expiração absoluta igual à do refresh e janela deslizante controlada de 30 minutos.
