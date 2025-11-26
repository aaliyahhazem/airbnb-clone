//Mapper
global using AutoMapper;
global using BLL.ModelVM.Notification;
global using DAL.Entities;

//notification service
global using BLL.ModelVM.Response;
global using BLL.Services.Abstractions;
global using DAL.Repo.Abstraction;

//modular
global using BLL.Helper;

global using BLL.AutoMapper;
global using BLL.Services.Impelementation;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.AspNetCore.Http;
global using System.ComponentModel.DataAnnotations;

global using BLL.ModelVM.Message;
global using BLL.ModelVM.ListingVM;
global using BLL.ModelVM.Admin;
global using BLL.ModelVM.Booking;
global using BLL.ModelVM.Payment;
global using BLL.ModelVM.Review;
global using DAL.Enum;
global using Microsoft.AspNetCore.Identity;

global using Microsoft.Extensions.Configuration;
global using Microsoft.IdentityModel.Tokens;
global using System.IdentityModel.Tokens.Jwt;
global using System.Security.Claims;
global  using System.Text;

//Stripe
global using Stripe;
global using Microsoft.Extensions.Options;
global using BLL.Helper;


//email service
global using BLL.ModelVM.Email;
global using MailKit.Net.Smtp;
global using MailKit.Security;
global using Microsoft.Extensions.Configuration;
global using MimeKit;

