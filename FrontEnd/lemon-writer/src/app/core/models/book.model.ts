export interface Book {
  id: string;
  title: string;
  description?: string;
  authorName: string;
  isbn?: string;
  inbr?: string;
  coverImageUrl?: string;
  isSeries: boolean;
  seriesName?: string;
  volumeNumber?: number;
  ownerId: string;
  createdAt: string;
  updatedAt: string;
  chapterCount?: number;
}

export interface CreateBookRequest {
  title: string;
  description?: string;
  authorName: string;
  isbn?: string;
  inbr?: string;
  isSeries: boolean;
  seriesName?: string;
  volumeNumber?: number;
}

export interface UpdateBookRequest extends Partial<CreateBookRequest> {}
