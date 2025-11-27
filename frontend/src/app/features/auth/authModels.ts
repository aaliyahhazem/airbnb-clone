export interface User{
  id:string;
  email:string;
  firstName:string;
  lastName:string;
  role: UserRole;
  avatar?:string;
}
export enum UserRole{
  GUEST = 'GUEST',
  HOST = 'HOST',
  ADMIN = 'ADMIN'
}
export interface AuthResponse{
  user:User;
  token:string;
}
export interface LoginCredentials{
  email:string;
  password:string;
}
export interface RegisterData{
  email: string;
  password: string;
  fullName: string;
  userName: string;
  firebaseUid: string; // will be "null" until firebase is implemented
}
export interface SocialLoginRequest {
  token: string;
}
