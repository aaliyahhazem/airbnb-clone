
export interface NotificationDto {
  id: number;
  userId: string;
  title: string;
  body: string;
  createdAt: string; // ISO
  isRead: boolean;
}
