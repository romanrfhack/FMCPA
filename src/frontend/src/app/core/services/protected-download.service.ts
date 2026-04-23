import { HttpClient, HttpResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ProtectedDownloadService {
  private readonly httpClient = inject(HttpClient);

  async download(url: string, fallbackFileName: string): Promise<void> {
    const response = await firstValueFrom(
      this.httpClient.get(url, {
        observe: 'response',
        responseType: 'blob'
      }));

    const objectUrl = globalThis.URL.createObjectURL(response.body ?? new Blob());
    const downloadLink = document.createElement('a');

    downloadLink.href = objectUrl;
    downloadLink.download = this.resolveFileName(response) ?? fallbackFileName;
    downloadLink.rel = 'noopener';
    downloadLink.style.display = 'none';

    document.body.appendChild(downloadLink);
    downloadLink.click();
    document.body.removeChild(downloadLink);

    globalThis.setTimeout(() => globalThis.URL.revokeObjectURL(objectUrl), 1_000);
  }

  private resolveFileName(response: HttpResponse<Blob>): string | null {
    const contentDisposition = response.headers.get('content-disposition');
    if (!contentDisposition) {
      return null;
    }

    const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition);
    if (utf8Match?.[1]) {
      return decodeURIComponent(utf8Match[1]);
    }

    const plainMatch = /filename="?([^";]+)"?/i.exec(contentDisposition);
    return plainMatch?.[1] ?? null;
  }
}
