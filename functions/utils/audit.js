'use strict';
async function auditLog(db,{action,actorType='system',uid=null,companyId=null,entity='',entityId='',before=null,after=null,ip=null,userAgent=null}){await db.collection('auditLogs').add({action,actorType,uid,companyId,entity,entityId,before,after,ip,userAgent,createdAt:new Date()});}
module.exports={auditLog};
