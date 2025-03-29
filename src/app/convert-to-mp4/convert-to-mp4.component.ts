import { Component } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';

@Component({
  selector: 'app-convert-to-mp4',
  templateUrl: './convert-to-mp4.component.html',
  styleUrl: './convert-to-mp4.component.scss'
})
export class ConvertToMp4Component {
  youtubeUrl: string = '';
  isLoading: boolean = false;
  downloadLink: string | null = null; // Added to align with HTML

  constructor(private http: HttpClient) {}

  convertToMp4() {
    if (!this.youtubeUrl) return;
  
    this.isLoading = true;
  
    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
    });
  
    this.http.post(
      'https://localhost:7173/api/Converter/convert-to-mp4', 
      { youtubeUrl: this.youtubeUrl, format: 'mp4' },
      { responseType: 'blob', observe: 'response', headers }
    ).subscribe(
      (response) => {
        this.isLoading = false;
  
        const blob = response.body!;
  
        // Extract filename from Content-Disposition header
        const contentDisposition = response.headers.get('Content-Disposition');
        const fileName = contentDisposition 
          ? contentDisposition.split('filename=')[1].replace(/"/g, '') 
          : 'converted.mp4'; // Fallback filename if header is missing
  
        // Create and trigger download link
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        link.click();
  
        // Cleanup URL object
        window.URL.revokeObjectURL(url);
        console.log('File downloaded successfully:', fileName);
      },
      (error) => {
        this.isLoading = false;
        console.error('Error during conversion:', error);
      }
    );
  }
  
}
