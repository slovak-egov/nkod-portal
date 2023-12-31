import ReactDOM from 'react-dom/client';
import App from './App';
import reportWebVitals from './reportWebVitals';
import './index.scss';
import { TokenResult } from './client';
import './i18n';

declare let externalToken: TokenResult|null;

// if (process.env.REACT_APP_TOKEN) {
//   externalToken = {token: process.env.REACT_APP_TOKEN, expires: null, refreshToken: '1', redirectUrl: null};
// }

const root = ReactDOM.createRoot(
  document.getElementById('root') as HTMLElement
);
root.render(
  <App extenalToken={externalToken} />
);

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals();
