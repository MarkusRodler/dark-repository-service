import http from 'k6/http';
import { check } from 'k6';
import * as config from './Config.js';

export const options = {
    vus: 30,
    duration: '60s',
};

export default () => {
    check(http.get(`${config.API_BASE_URL}Read/MenuService/Meal.jsonl`), { 'is status 200': (r) => r.status === 200 });
};
