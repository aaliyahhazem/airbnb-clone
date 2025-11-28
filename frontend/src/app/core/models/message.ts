export interface MessageDto {
  id: number;
  senderId: string; // GUID as string
  receiverId: string;
  content: string;
  sentAt: string; // ISO
  isRead: boolean;
}
