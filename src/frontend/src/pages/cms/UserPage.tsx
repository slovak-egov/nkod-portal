import Breadcrumbs from "../../components/Breadcrumbs";
import MainContent from "../../components/MainContent";
import PageHeader from "../../components/PageHeader";
import {useTranslation} from "react-i18next";
import Button from "../../components/Button";
import {useLocation, useNavigate} from "react-router";
import Alert from "../../components/Alert";
import {CmsUserContext, useCmsUserLogout} from "../../cms";
import PageSubheader from "../../components/PageSubHeader";
import React, {useContext} from "react";

export default function UserPage() {
    const cmsUserContext = useContext(CmsUserContext);
    const navigate = useNavigate();
    const location = useLocation();
    const {t} = useTranslation();
    const [genericError, saving, save] = useCmsUserLogout();

    return <>
        <Breadcrumbs items={[{title: t('nkod'), link: '/'}, {title: t('odCommunity')}]}/>
        {location.state?.info ?
            <Alert type={'info'}>
                {location.state.info}
            </Alert>
            : null}

        <MainContent>
            <PageHeader>{t('odCommunity')}</PageHeader>
            <PageSubheader>{t('welcome')} {cmsUserContext?.cmsUser?.userName}</PageSubheader>


            <Button style={{marginRight: '20px'}} onClick={async () => {
                const result = await save();
                cmsUserContext?.setCmsUser(null);
                if (result?.success) {
                    navigate('/odkomunita', {state: {info: t('logoutSuccessful')}});
                }
            }}>
                {t('logout')}
            </Button>

        </MainContent>
    </>;
}