import { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useUserInfo } from '../client';
import Loading from './Loading';

type Props = {
    children: ReactNode;
};

export default function RequireLogin(props: Props) {
    const [userInfo, userInfoLoading] = useUserInfo();
    const location = useLocation();

    if (userInfo) {
        return <>{props.children}</>;
    }

    if (userInfoLoading) {
        return <Loading />;
    }

    return <Navigate to="/prihlasenie" state={{ from: location }} replace />;
}
