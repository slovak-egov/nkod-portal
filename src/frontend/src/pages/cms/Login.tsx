import {useTranslation} from "react-i18next";
import {useNavigate} from "react-router";
import Breadcrumbs from "../../components/Breadcrumbs";
import PageHeader from "../../components/PageHeader";
import MainContent from "../../components/MainContent";
import FormElementGroup from "../../components/FormElementGroup";
import BaseInput from "../../components/BaseInput";
import {CmsUserContext, getCmsUser, useCmsUserLogin} from "../../cms";
import Alert from "../../components/Alert";
import ErrorAlert from "../../components/ErrorAlert";
import Button from "../../components/Button";
import React, {useContext} from "react";

export default function Login() {
    const cmsUserContext = useContext(CmsUserContext);
    const navigate = useNavigate();
    const {t} = useTranslation();
    const [user, setUser, genericError, saving, save] = useCmsUserLogin({
        username: '',
        password: '',
    });

    return <>
        <Breadcrumbs items={[{title: t('nkod'), link: '/'}, {
            title: t('odCommunity'),
            link: '/odkomunita'
        }, {title: t('login')}]}/>
        <MainContent>
            <PageHeader>{t('login')}</PageHeader>
            <FormElementGroup label={t('userName')}
                              element={id => <BaseInput id={id} disabled={saving} value={user.username}
                                                        onChange={e => setUser({username: e.target.value})}/>}/>
            <FormElementGroup label={t('password')}
                              element={id => <BaseInput type={'password'} id={id} disabled={saving}
                                                        value={user.password}
                                                        onChange={e => setUser({password: e.target.value})}/>}/>
            {genericError ?
                <Alert type={'warning'}>
                    <ErrorAlert error={genericError}/>
                </Alert>
                : null}
            <Button style={{marginRight: '20px'}} onClick={async () => {
                const result = await save();
                cmsUserContext?.setCmsUser(await getCmsUser())
                if (result?.success) {
                    navigate('/odkomunita/user-page', {state: {info: t('loginSuccessful')}});
                }
            }}>
                {t('login')}
            </Button>

        </MainContent>
    </>;
}