$ErrorActionPreference='Stop'
$name = Read-Host 'Nome do SuperAdmin'
$email = Read-Host 'E-mail do SuperAdmin'
$secure = Read-Host 'Senha do SuperAdmin' -AsSecureString
Write-Host 'Use o utilitário oficial de hash do projeto para gerar PasswordHash; a senha não será exibida.'
$hash = Read-Host 'PasswordHash gerado'
$sql = "insert into habitflow.users(name,email,password_hash,role) values ('$($name.Replace("'","''"))','$($email.Replace("'","''"))','$($hash.Replace("'","''"))','SuperAdmin') on conflict (email) do update set name=excluded.name, password_hash=excluded.password_hash, role='SuperAdmin';"
Write-Host $sql
Write-Host 'Execute o SQL acima com psql em ambiente seguro. Não commite dados reais.'
