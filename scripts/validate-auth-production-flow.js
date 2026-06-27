const {read,exists,assert,pass}=require('./validate-sprint27-utils');
assert(exists('backend/Valora.Application/DTOs/Auth/PasswordResetRequests.cs'),'password reset DTOs missing');
const auth=read('backend/Valora.Application/Services/Auth/AuthService.cs');
const ctl=read('backend/Valora.Api/Controllers/AuthController.cs');
assert(ctl.includes('/auth/forgot-password')&&ctl.includes('/auth/reset-password'),'auth reset endpoints missing');
assert(auth.includes('CreatePasswordResetTokenAsync')&&auth.includes('HashToken')&&auth.includes('expiresAt'),'hashed expiring reset token flow missing');
assert(!auth.includes('resetToken = rawToken'),'raw reset token persisted in payload');
pass('validate-auth-production-flow');
