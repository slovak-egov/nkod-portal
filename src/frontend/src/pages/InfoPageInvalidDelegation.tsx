import { useTranslation } from "react-i18next";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import PageHeader from "../components/PageHeader";
import { useDocumentTitle } from "../client";

export default function InfoPageInvalidDelegation()
{
    const {t} = useTranslation();
    useDocumentTitle(t('permissionDelegation'));

    return <>
        <Breadcrumbs items={[{title: t('nkod'), link: '/'}]} />
            <MainContent>
                <PageHeader>{t('permissionDelegation')}</PageHeader>
                <p className="govuk-body">
                    {t('delegationPermissionError')}
                </p>
            </MainContent>
        </>;
}