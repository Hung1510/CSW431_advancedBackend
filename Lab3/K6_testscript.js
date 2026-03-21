import http from 'k6/http';
import { sleep } from 'k6';
export let options = { vus: 50, duration: '30s' };
export default function () {
    //http.get('http://localhost:5236/api/books/sync');
    //http.get('http://localhost:5236/api/books/baseline');
    //http.get('http://localhost:5236/api/books/optimized/cached');
    //http.get('http://localhost:5236/api/statistics/borrowing/sequential');
    //http.get('http://localhost:5236/api/statistics/borrowing/parallel-async');
    //http.get('http://localhost:5236/api/statistics/cpu-intensive/sequential');
    //http.get('http://localhost:5236/api/statistics/cpu-intensive/multithreaded');
    sleep(0.1);
}