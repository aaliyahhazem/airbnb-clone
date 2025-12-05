global using BLL.ModelVM.Notification;
global using BLL.Services.Abstractions;
global using Microsoft.AspNetCore.Authorization;

global using Microsoft.AspNetCore.Mvc;
global using DAL.Enum;
global using BLL.ModelVM.Auth;
global using BLL.ModelVM.Booking;
global using System.Security.Claims;
global using System.IdentityModel.Tokens.Jwt;

global using BLL.ModelVM.Message;
global using Microsoft.AspNetCore.SignalR;
global using PL.Hubs;

global using BLL.ModelVM.Payment;
global using BLL.ModelVM.Review;

global using System.Collections.Concurrent;
global using BLL.Services.Impelementation;
global using BLL.Common;
global using DAL.Common;
global using DAL.Database;
global using DAL.Entities;

global using Microsoft.AspNetCore.Identity;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.AspNetCore.Authentication.JwtBearer;
global using Microsoft.IdentityModel.Tokens;
global using System.Text;
global using BLL.ModelVM.ListingVM;


global using BLL.AutoMapper;
global using BLL.Helper;
global using Stripe;
global using Microsoft.AspNetCore.Localization;

global using FirebaseAdmin;
global using FirebaseAdmin.Auth;
global using Google.Apis.Auth.OAuth2;
global using BLL.ModelVM.Response;
global using DAL.Repo.Abstraction;

