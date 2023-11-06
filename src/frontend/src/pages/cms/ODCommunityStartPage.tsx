import Breadcrumbs from "../../components/Breadcrumbs";
import MainContent from "../../components/MainContent";
import PageHeader from "../../components/PageHeader";
import {useTranslation} from "react-i18next";
import Button from "../../components/Button";
import {useLocation, useNavigate} from "react-router";
import Alert from "../../components/Alert";

export default function ODCommunityStartPage() {
    const navigate = useNavigate();
    const location = useLocation();
    const { t } = useTranslation();

    return <>
        <Breadcrumbs items={[{title: t('nkod'), link: '/'}, {title: t('odCommunity')}]}/>
        {location.state?.info ?
            <Alert type={'info'}>
                {location.state.info}
            </Alert>
            : null}

        <MainContent>
            <PageHeader>{t('odCommunity')}</PageHeader>

            <Button style={{marginRight: '20px'}} onClick={() => navigate('/odkomunita/register-user')}
                    buttonType={"secondary"}>{t('register')}</Button>
            <Button onClick={() => navigate('/odkomunita/login')} buttonType={"primary"}>{t('login')}</Button>
            
        </MainContent>
    </>;
}