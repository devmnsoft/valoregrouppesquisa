const express=require('express');const {config}=require('../config');const router=express.Router();
router.get('/health',(_req,res)=>res.json({ok:true,service:'valora-communication-gateway',environment:config.env,time:new Date().toISOString()}));
module.exports=router;
