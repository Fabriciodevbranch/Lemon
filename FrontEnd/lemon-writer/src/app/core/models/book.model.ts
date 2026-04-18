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
  seriesVolume?: number;
  authorId: string;
  createdAt: string;
  updatedAt: string;
  chapterCount?: number;
}

export interface CreateBookRequest {
  title: string;
  description?: string;
  authorId: string;
  authorName: string;
  isbn?: string;
  inbr?: string;
  isSeries: boolean;
  seriesName?: string;
  seriesVolume?: number;
}

export interface UpdateBookRequest {
  title: string;
  description?: string;
  authorName: string;
  isbn?: string;
  inbr?: string;
  isSeries: boolean;
  seriesName?: string;
  seriesVolume?: number;
}
