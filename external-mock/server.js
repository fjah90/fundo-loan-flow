// External-service mock: in-memory customer store, upsert by customerId (AR-15, AR-49).
const http = require('node:http');

const store = new Map();
let failNext = 0;

const send = (res, status, body) => {
  res.writeHead(status, { 'content-type': 'application/json' });
  res.end(JSON.stringify(body ?? {}));
};

http.createServer((req, res) => {
  const { method } = req;
  const path = req.url.split('?')[0];
  let customerId = '-';
  const done = (status, body) => {
    // Never log the body or the SSN: method, path, customerId and status only.
    console.log(`${method} ${path} customerId=${customerId} -> ${status}`);
    send(res, status, body);
  };

  const fail = path.match(/^\/_admin\/fail-next\/([^/]*)$/);
  if (method === 'POST' && fail) {
    if (!/^\d+$/.test(fail[1])) return done(400, { error: 'n must be an integer >= 0' });
    failNext = Number(fail[1]);
    return done(200, { failNext });
  }

  const put = path.match(/^\/customers\/([^/]+)$/);
  if (method === 'GET' && path === '/customers') return done(200, [...store.values()]);
  if (!((method === 'POST' && path === '/customers') || (method === 'PUT' && put))) {
    return done(404, { error: 'not found' });
  }

  let raw = '';
  req.on('data', (c) => (raw += c));
  req.on('end', () => {
    if (put) {
      try { customerId = decodeURIComponent(put[1]); } catch { return done(400, { error: 'invalid customer id' }); }
    }
    if (failNext > 0) { failNext--; return done(503, { error: 'simulated outage' }); }
    let body;
    try { body = JSON.parse(raw); } catch { return done(400, { error: 'invalid JSON' }); }
    if (body === null || typeof body !== 'object' || Array.isArray(body)) return done(400, { error: 'body must be an object' });
    if (!put) {
      if (typeof body.customerId !== 'string' || !body.customerId) return done(400, { error: 'customerId required' });
      customerId = body.customerId;
    }
    const record = { ...body, customerId };
    store.set(customerId, record);
    done(200, record);
  });
}).listen(process.env.PORT || 4000, () => console.log('external-mock listening'));
