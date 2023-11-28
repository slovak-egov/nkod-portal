import { useTranslation } from "react-i18next";
import IdSkModule from "./IdSkModule";

export default function SearchInput() {
    const {t} = useTranslation();
    return <IdSkModule moduleType="idsk-search-component" className="idsk-search-component">
            <input className="govuk-input idsk-search-component__input"
            placeholder={t('enterSearchTerm')}
            title={t('enterSearchTerm')} type="search" aria-label={t('enterSearchTerm')} />
        <button className="idsk-button idsk-search-component__button">
            <span className="govuk-visually-hidden">{t('doSearch')}</span>
            <i aria-hidden="true" className="fas fa-search"></i>
        </button>
    </IdSkModule>;
}