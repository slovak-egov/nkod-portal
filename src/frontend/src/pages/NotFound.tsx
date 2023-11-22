import { useTranslation } from "react-i18next";
import { useDocumentTitle } from "../client";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import PageHeader from "../components/PageHeader";

export default function NotFound()
{
    const {t} = useTranslation();
    useDocumentTitle(t('notFound'));

    return <>
        <Breadcrumbs items={[{title: t('nkod'), link: '/'}]} />
            <MainContent>
                <PageHeader>{t('notFound')}</PageHeader>

                <p className="govuk-body">
                    {t('pageNotFound')}
                </p>
            </MainContent>
        </>;
}