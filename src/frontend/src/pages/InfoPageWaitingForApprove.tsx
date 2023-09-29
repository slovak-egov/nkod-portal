import { useTranslation } from "react-i18next";
import { useDocumentTitle, useUserInfo } from "../client";
import Breadcrumbs from "../components/Breadcrumbs";
import MainContent from "../components/MainContent";
import PageHeader from "../components/PageHeader";

export default function InfoPageWaitingForApprove()
{
    const [userInfo] = useUserInfo();
    const {t} = useTranslation();
    useDocumentTitle(t('publisherApprove'));

    return <>
        <Breadcrumbs items={[{title: t('nkod'), link: '/'}]} />
            <MainContent>
                <PageHeader>{t('publisherApprove')}</PageHeader>
                {userInfo?.publisherView ? <p className="govuk-body nkod-publisher-name">
                <span style={{color: '#2B8CC4', fontWeight: 'bold'}}>{t('publisher')}</span><br />
                    {userInfo.publisherView.name}
                </p> : null}

                <p className="govuk-body">
                    {t('waitingForApprove')}
                </p>
            </MainContent>
        </>;
}