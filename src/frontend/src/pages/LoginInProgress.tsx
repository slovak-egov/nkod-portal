import { useTranslation } from 'react-i18next';
import { useDocumentTitle } from '../client';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import PageHeader from '../components/PageHeader';

export default function LoginInProgress() {
    const { t } = useTranslation();
    useDocumentTitle(t('loginInProgress'));

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }]} />
            <MainContent>
                <PageHeader>{t('loginInProgress')}</PageHeader>

                <p className="govuk-body">{t('loginInProgress')}</p>
            </MainContent>
        </>
    );
}
