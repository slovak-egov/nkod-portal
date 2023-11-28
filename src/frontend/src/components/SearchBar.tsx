import { useTranslation } from "react-i18next";
import IdSkModule from "./IdSkModule";
import { InputHTMLAttributes, useId } from "react";

export default function SearchBar(props: InputHTMLAttributes<HTMLInputElement>) {
    const id = useId();
    const {t} = useTranslation();

    return <div className="idsk-search-results__search-bar">
        <IdSkModule moduleType="idsk-search-component" className="idsk-search-component idsk-search-component--small">
            <label className="idsk-search-component__label--small" htmlFor={id} style={{display: 'none'}}>
                {t('enterSearchTerm')}
            </label>
            <input className="govuk-input idsk-search-component__input idsk-search-component__input--small" placeholder={t('enterSearchTerm')} id={id} type="search" {...props} />
            <button className="idsk-button idsk-search-component__button idsk-search-component__button--small">
                <svg width="18" height="18" viewBox="0 0 31 30" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <path d="M21.0115 13.103C21.0115 17.2495 17.5484 20.6238 13.2928 20.6238C9.03714 20.6238 5.57404 17.2495 5.57404 13.103C5.57404
                        8.95643 9.03714 5.58212 13.2928 5.58212C17.5484 5.58212 21.0115 8.95643 21.0115 13.103ZM29.833 27.0702C29.833 26.4994
                        29.5918 25.9455 29.1955 25.5593L23.2858 19.8012C24.6814 17.8371 25.4223 15.4868 25.4223 13.103C25.4223 6.57259 19.995
                        1.28451 13.2928 1.28451C6.59058 1.28451 1.16333 6.57259 1.16333 13.103C1.16333 19.6333 6.59058 24.9214 13.2928
                        24.9214C15.7394 24.9214 18.1515 24.1995 20.1673 22.8398L26.077 28.5811C26.4732 28.984 27.0418 29.219 27.6276
                        29.219C28.8337 29.219 29.833 28.2453 29.833 27.0702Z" fill="white"></path>
                    <path fillRule="evenodd" clipRule="evenodd" d="M0.75708 13.103C0.75708 6.35398 6.36621 0.888672 13.2928 0.888672C20.2194 0.888672 25.8285 6.35398 25.8285
                        13.103C25.8285 15.4559 25.1301 17.7778 23.8094 19.7516L29.4827 25.2794C29.9551 25.7396 30.2392 26.3943 30.2392
                        27.0702C30.2392 28.464 29.058 29.6149 27.6276 29.6149C26.9347 29.6149 26.2611 29.3385 25.787 28.8584L20.1168
                        23.3497C18.0909 24.6367 15.7078 25.3172 13.2928 25.3172C6.36621 25.3172 0.75708 19.8519 0.75708 13.103ZM13.2928
                        1.68034C6.81494 1.68034 1.56958 6.7912 1.56958 13.103C1.56958 19.4147 6.81494 24.5256 13.2928 24.5256C15.6581 24.5256
                        17.9892 23.8275 19.9361 22.5143L20.2144 22.3265L26.3704 28.3071C26.6886 28.6308 27.1506 28.8232 27.6276 28.8232C28.6093
                        28.8232 29.4267 28.0267 29.4267 27.0702C29.4267 26.6046 29.2285 26.1513 28.9082 25.8392L22.7588 19.8475L22.9518
                        19.5759C24.2996 17.679 25.016 15.4076 25.016 13.103C25.016 6.7912 19.7706 1.68034 13.2928 1.68034ZM13.2928
                        5.97796C9.26151 5.97796 5.98029 9.17504 5.98029 13.103C5.98029 17.0309 9.26151 20.228 13.2928 20.228C17.3241 20.228
                        20.6053 17.0309 20.6053 13.103C20.6053 9.17504 17.3241 5.97796 13.2928 5.97796ZM5.16779 13.103C5.16779 8.73781 8.81278
                        5.18629 13.2928 5.18629C17.7728 5.18629 21.4178 8.73781 21.4178 13.103C21.4178 17.4681 17.7728 21.0196 13.2928
                        21.0196C8.81278 21.0196 5.16779 17.4681 5.16779 13.103Z" fill="white"></path>
                </svg>
                <span className="govuk-visually-hidden">{t('search')}</span>
            </button>
        </IdSkModule>
    </div>;
}